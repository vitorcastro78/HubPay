using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using HubPay.Infrastructure.Payments.Webhooks;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class MBWayStrategy : PaymentStrategyBase
{
    private readonly IHubPaySettingsProvider _settingsProvider;

    public MBWayStrategy(
        HttpClient httpClient,
        ILogger<MBWayStrategy> logger,
        ITransactionRepository repository,
        IHubPaySettingsProvider settingsProvider) : base(httpClient, logger, repository)
    {
        _settingsProvider = settingsProvider;
    }

    public override string SchemeName => "MBWAY";
    private SibsApiSettings Settings => _settingsProvider.Current.Sibs;
    private PspApiClient Api => new(HttpClient, Settings, SchemeName, Logger);

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        PspStrategyHelper.EnsureProductionReady(Settings, SchemeName);
        var phone = PspPhoneValidator.RequirePhone(transaction.CustomerPhone, SchemeName);

        var payload = new
        {
            merchantTransactionId = transaction.Id.ToString(),
            amount = new { value = transaction.Amount, currency = transaction.Currency },
            customer = new { email = transaction.CustomerEmail, mobilePhone = phone },
            paymentMethod = "MBWAY",
            callbackUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
        };

        try
        {
            var response = await Api.PostAsync(Settings.MbWayInitPath, payload, ct);
            var externalRef = PspStrategyHelper.ReadString(response, "paymentId", "id")
                              ?? transaction.Id.ToString();
            var redirectUrl = PspStrategyHelper.ReadString(response, "redirectUrl", "paymentUrl");

            Logger.LogInformation("MB WAY SIBS iniciado ref={Ref} phone={Phone}", externalRef, phone);
            return new PaymentResult(true, externalRef, "Pending", redirectUrl, JsonSerializer.Serialize(payload));
        }
        catch (PspIntegrationException ex) when (Settings.EnableSimulationFallback)
        {
            return PspStrategyHelper.BuildFallback(transaction, SchemeName, "Pending", null, payload, null, Logger, ex);
        }
    }

    public override Task<WebhookResult> HandleWebhookAsync(
        string payload, Dictionary<string, string> headers, CancellationToken ct) =>
        PspWebhookProcessor.ProcessAsync(
            SchemeName, payload, Repository, Logger,
            root => PspJson.ReadString(root, "paymentId", "id"), ct);

    public override async Task<RefundResult> RefundAsync(Guid transactionId, decimal amount, CancellationToken ct)
    {
        var transaction = await Repository.GetByIdAsync(transactionId, ct);
        if (transaction is null)
            return new RefundResult(false, string.Empty, amount, "NOT_FOUND");

        if (string.IsNullOrWhiteSpace(transaction.ExternalReference))
            return await base.RefundAsync(transactionId, amount, ct);

        var path = Settings.RefundPath.Replace("{paymentId}", transaction.ExternalReference, StringComparison.Ordinal);
        var body = new { amount = new { value = amount, currency = transaction.Currency }, reason = "MERCHANT_INITIATED" };

        try
        {
            var response = await Api.PostAsync(path, body, ct);
            var refundId = PspStrategyHelper.ReadString(response, "refundId", "id") ?? $"RF-MBW-{Guid.NewGuid():N}"[..24];
            return new RefundResult(true, refundId, amount, "REFUNDED");
        }
        catch (PspIntegrationException ex) when (Settings.EnableSimulationFallback)
        {
            Logger.LogWarning(ex, "Reembolso MB WAY em fallback simulado");
            return await base.RefundAsync(transactionId, amount, ct);
        }
    }
}
