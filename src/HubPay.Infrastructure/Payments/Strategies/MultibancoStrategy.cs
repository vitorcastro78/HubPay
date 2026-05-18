using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class MultibancoStrategy : PaymentStrategyBase
{
    private readonly SibsApiSettings _settings;
    private readonly PspApiClient _api;

    public MultibancoStrategy(
        HttpClient httpClient,
        ILogger<MultibancoStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options) : base(httpClient, logger, repository)
    {
        _settings = options.Value.Sibs;
        _api = new PspApiClient(httpClient, _settings, SchemeName, logger);
    }

    public override string SchemeName => "MULTIBANCO";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var (entity, reference, dueDate) = MultibancoReferenceGenerator.Generate(transaction.Amount, transaction.MerchantId);
        var payload = new
        {
            merchantTransactionId = transaction.Id.ToString(),
            entity,
            reference,
            amount = new { value = transaction.Amount, currency = transaction.Currency },
            dueDate = dueDate.ToString("yyyy-MM-dd"),
            endToEndId = transaction.EndToEndId,
            notificationUrl = PspStrategyHelper.WebhookUrl(_settings, SchemeName)
        };

        try
        {
            var response = await _api.PostAsync(_settings.MultibancoInitPath, payload, ct);
            var externalRef = PspStrategyHelper.ReadString(response, "paymentReference", "id")
                              ?? $"{entity}/{reference}";

            Logger.LogInformation("Multibanco SIBS ref={Ref} mTLS={Mtls}", externalRef, _settings.MutualTls.Enabled);
            return new PaymentResult(true, externalRef, "Pending", null, JsonSerializer.Serialize(payload));
        }
        catch (PspIntegrationException ex) when (_settings.EnableSimulationFallback)
        {
            Logger.LogWarning(ex, "Multibanco SIBS indisponível — referência local");
            return new PaymentResult(true, $"{entity}/{reference}", "Pending", null, JsonSerializer.Serialize(payload));
        }
    }
}
