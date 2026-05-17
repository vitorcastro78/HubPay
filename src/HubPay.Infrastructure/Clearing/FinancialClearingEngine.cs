using System.Xml.Linq;
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
        var authorized = await repository.GetByStatusAsync(TransactionStatus.Authorized, ct);
        if (authorized.Count == 0) return;

        var camtXml = BuildSimulatedCamt053(authorized);
        var doc = XDocument.Parse(camtXml);
        var ns = XNamespace.Get("urn:iso:std:iso:20022:tech:xsd:camt.053.001.08");

        foreach (var entry in doc.Descendants(ns + "Ntry"))
        {
            var endToEndId = entry.Descendants(ns + "EndToEndId").FirstOrDefault()?.Value;
            var amountEl = entry.Descendants(ns + "Amt").FirstOrDefault();
            if (endToEndId is null || amountEl is null) continue;

            if (!decimal.TryParse(amountEl.Value, out var bankAmount)) continue;

            var transaction = authorized.FirstOrDefault(t => t.EndToEndId == endToEndId);
            if (transaction is null) continue;

            if (transaction.Amount != bankAmount)
            {
                _logger.LogWarning("Montante divergente E2E={E2E} esperado={Expected} banco={Bank}",
                    endToEndId, transaction.Amount, bankAmount);
                continue;
            }

            var fee = Math.Round(transaction.Amount * (_settings.HubProcessingFeePercent / 100m), 2);
            var net = transaction.Amount - fee;
            transaction.Settle(net, fee);
            await repository.UpdateAsync(transaction, ct);

            _logger.LogInformation(
                "Liquidação ISO 20022 E2E={E2E} bruto={Gross} taxa={Fee} líquido={Net}",
                endToEndId, transaction.Amount, fee, net);
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
