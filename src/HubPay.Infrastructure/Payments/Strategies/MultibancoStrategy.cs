using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using HubPay.Infrastructure.Payments.Webhooks;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class MultibancoStrategy : PaymentStrategyBase
{
    private readonly IHubPaySettingsProvider _settingsProvider;

    public MultibancoStrategy(
        HttpClient httpClient,
        ILogger<MultibancoStrategy> logger,
        ITransactionRepository repository,
        IHubPaySettingsProvider settingsProvider) : base(httpClient, logger, repository)
    {
        _settingsProvider = settingsProvider;
    }

    public override string SchemeName => "MULTIBANCO";
    private SibsApiSettings Settings => _settingsProvider.Current.Sibs;
    private PspApiClient Api => new(HttpClient, Settings, SchemeName, Logger);

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        PspStrategyHelper.EnsureProductionReady(Settings, SchemeName);
        var entity = Settings.ResolveMultibancoEntity(transaction.MerchantId);
        var (entityCode, reference, dueDate) = MultibancoReferenceGenerator.Generate(transaction.Amount, transaction.MerchantId, entity);

        var payload = new
        {
            merchantTransactionId = transaction.Id.ToString(),
            entity = entityCode,
            reference,
            amount = new { value = transaction.Amount, currency = transaction.Currency },
            dueDate = dueDate.ToString("yyyy-MM-dd"),
            endToEndId = transaction.EndToEndId,
            notificationUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
        };

        var details = new PaymentSchemeDetails(entityCode, reference, dueDate);

        try
        {
            var response = await Api.PostAsync(Settings.MultibancoInitPath, payload, ct);
            var externalRef = PspStrategyHelper.ReadString(response, "paymentReference", "id")
                              ?? $"{entityCode}/{reference}";

            Logger.LogInformation("Multibanco SIBS entidade={Entity} ref={Ref}", entityCode, reference);
            return new PaymentResult(true, externalRef, "Pending", null, JsonSerializer.Serialize(payload), details);
        }
        catch (PspIntegrationException ex) when (Settings.EnableSimulationFallback)
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
