using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using HubPay.Infrastructure.Payments.Webhooks;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public abstract class PspEndpointStrategyBase : PaymentStrategyBase
{
    private readonly IHubPaySettingsProvider _settingsProvider;
    private readonly Func<HubPay.Domain.Configuration.HubPaySettings, PspEndpointSettings> _settingsSelector;

    protected PspEndpointStrategyBase(
        HttpClient httpClient,
        IHubPaySettingsProvider settingsProvider,
        Func<HubPay.Domain.Configuration.HubPaySettings, PspEndpointSettings> settingsSelector,
        string schemeName,
        ILogger logger,
        ITransactionRepository repository) : base(httpClient, logger, repository)
    {
        _settingsProvider = settingsProvider;
        _settingsSelector = settingsSelector;
        SchemeName = schemeName;
    }

    public override string SchemeName { get; }

    protected PspEndpointSettings Settings => _settingsSelector(_settingsProvider.Current);
    protected PspApiClient Api => new(HttpClient, Settings, SchemeName, Logger);

    protected abstract string PaymentInitPath { get; }
    protected abstract string? DefaultRedirectUrl { get; }

    protected virtual object BuildPaymentPayload(Transaction transaction) => new
    {
        merchantId = Settings.MerchantId.Length > 0 ? Settings.MerchantId : transaction.MerchantId,
        amount = new { value = transaction.Amount, currency = transaction.Currency },
        endToEndId = transaction.EndToEndId,
        notificationUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
    };

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        PspStrategyHelper.EnsureProductionReady(Settings, SchemeName);
        var payload = BuildPaymentPayload(transaction);

        try
        {
            var response = await Api.PostAsync(PaymentInitPath, payload, ct);
            return MapPaymentResult(transaction, response, payload);
        }
        catch (PspIntegrationException ex) when (Settings.EnableSimulationFallback)
        {
            return PspStrategyHelper.BuildFallback(
                transaction, SchemeName, "Pending", DefaultRedirectUrl, payload, null, Logger, ex);
        }
    }

    public override Task<WebhookResult> HandleWebhookAsync(
        string payload,
        Dictionary<string, string> headers,
        CancellationToken ct) =>
        PspWebhookProcessor.ProcessAsync(
            SchemeName, payload, Repository, Logger,
            root => PspJson.ReadString(root, "paymentId", "id", "transactionId"),
            ct);

    protected virtual PaymentResult MapPaymentResult(Transaction transaction, JsonElement response, object payload)
    {
        var externalRef = PspStrategyHelper.ReadString(response, "paymentId", "id", "transactionId")
                          ?? $"{SchemeName[..Math.Min(3, SchemeName.Length)]}-{transaction.Id:N}"[..20];
        var redirectUrl = PspStrategyHelper.ReadString(response, "redirectUrl", "paymentUrl", "authorizationUrl")
                          ?? DefaultRedirectUrl;

        Logger.LogInformation("{Scheme} PSP ref={Ref} mTLS={Mtls}", SchemeName, externalRef, Settings.MutualTls.Enabled);
        return new PaymentResult(true, externalRef, "Pending", redirectUrl, JsonSerializer.Serialize(payload));
    }
}
