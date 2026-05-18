using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using HubPay.Infrastructure.Payments.Webhooks;
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
        PspStrategyHelper.EnsureProductionReady(_settings, SchemeName);
        var entity = _settings.ResolveMultibancoEntity(transaction.MerchantId);
        var (entityCode, reference, dueDate) = MultibancoReferenceGenerator.Generate(transaction.Amount, transaction.MerchantId, entity);

        var payload = new
        {
            merchantTransactionId = transaction.Id.ToString(),
            entity = entityCode,
            reference,
            amount = new { value = transaction.Amount, currency = transaction.Currency },
            dueDate = dueDate.ToString("yyyy-MM-dd"),
            endToEndId = transaction.EndToEndId,
            notificationUrl = PspStrategyHelper.WebhookUrl(_settings, SchemeName)
        };

        var details = new PaymentSchemeDetails(entityCode, reference, dueDate);

        try
        {
            var response = await _api.PostAsync(_settings.MultibancoInitPath, payload, ct);
            var externalRef = PspStrategyHelper.ReadString(response, "paymentReference", "id")
                              ?? $"{entityCode}/{reference}";

            Logger.LogInformation("Multibanco SIBS entidade={Entity} ref={Ref}", entityCode, reference);
            return new PaymentResult(true, externalRef, "Pending", null, JsonSerializer.Serialize(payload), details);
        }
        catch (PspIntegrationException ex) when (_settings.EnableSimulationFallback)
        {
            return PspStrategyHelper.BuildFallback(
                transaction, SchemeName, "Pending", null, payload, details, Logger, ex);
        }
    }

    public override Task<WebhookResult> HandleWebhookAsync(
        string payload, Dictionary<string, string> headers, CancellationToken ct) =>
        PspWebhookProcessor.ProcessAsync(
            SchemeName, payload, Repository, Logger,
            root => PspJson.ReadString(root, "paymentReference", "paymentId", "id"), ct);
}
