using HubPay.Domain.Configuration;
using HubPay.Domain.Enums;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Clearing;

public sealed class FinancialClearingEngine : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FinancialClearingEngine> _logger;
    private readonly HubPaySettings _settings;

    public FinancialClearingEngine(
        IServiceScopeFactory scopeFactory,
        IOptions<HubPaySettings> options,
        ILogger<FinancialClearingEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessClearingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no motor de reconciliação ISO 20022");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.ClearingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessClearingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var reader = scope.ServiceProvider.GetRequiredService<ICamt053StatementReader>();
        var parser = scope.ServiceProvider.GetRequiredService<ICamt053Parser>();
        var notifier = scope.ServiceProvider.GetService<ITransactionNotifier>();

        var authorized = await repository.GetByStatusAsync(TransactionStatus.Authorized, ct);
        if (authorized.Count == 0) return;

        var statements = await reader.ReadPendingStatementsAsync(ct);
        if (statements.Count == 0 && _settings.Camt053.UseSimulatedStatementsWhenEmpty)
        {
            statements = [new Camt053Statement("simulated.xml", BuildSimulatedCamt053(authorized))];
        }

        foreach (var statement in statements)
        {
            var entries = parser.ParseEntries(statement.XmlContent);
            foreach (var entry in entries)
            {
                var transaction = authorized.FirstOrDefault(t => t.EndToEndId == entry.EndToEndId);
                if (transaction is null) continue;

                if (transaction.Amount != entry.Amount)
                {
                    _logger.LogWarning(
                        "Montante divergente E2E={E2E} esperado={Expected} banco={Bank}",
                        entry.EndToEndId, transaction.Amount, entry.Amount);
                    continue;
                }

                var fee = Math.Round(transaction.Amount * (_settings.HubProcessingFeePercent / 100m), 2);
                var net = transaction.Amount - fee;
                transaction.Settle(net, fee);
                await repository.UpdateAsync(transaction, ct);

                if (notifier is not null)
                    await notifier.NotifyUpdatedAsync(transaction, ct);

                _logger.LogInformation(
                    "Liquidação ISO 20022 ficheiro={File} E2E={E2E} bruto={Gross} taxa={Fee} líquido={Net}",
                    statement.FileName, entry.EndToEndId, transaction.Amount, fee, net);
            }

            if (statement.FileName != "simulated.xml")
                await reader.MarkAsProcessedAsync(statement.FileName, ct);
        }
    }

    private static string BuildSimulatedCamt053(IReadOnlyList<Domain.Entities.Transaction> transactions)
    {
        var entries = string.Join("", transactions.Select(t => $@"
    <Ntry>
      <Amt Ccy=""EUR"">{t.Amount:F2}</Amt>
      <CdtDbtInd>CRDT</CdtDbtInd>
      <NtryDtls>
        <TxDtls>
          <Refs>
            <EndToEndId>{t.EndToEndId}</EndToEndId>
          </Refs>
        </TxDtls>
      </NtryDtls>
    </Ntry>"));

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Document xmlns=""urn:iso:std:iso:20022:tech:xsd:camt.053.001.08"">
  <BkToCstmrStmt>
    <Stmt>
      <Id>STMT-{DateTime.UtcNow:yyyyMMddHHmmss}</Id>
      {entries}
    </Stmt>
  </BkToCstmrStmt>
</Document>";
    }
}
