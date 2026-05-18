using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using HubPay.Infrastructure.Payments.Webhooks;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class WeroStrategy : PaymentStrategyBase
{
    private readonly IHubPaySettingsProvider _settingsProvider;

    public WeroStrategy(
        HttpClient httpClient,
        ILogger<WeroStrategy> logger,
        ITransactionRepository repository,
        IHubPaySettingsProvider settingsProvider) : base(httpClient, logger, repository)
    {
        _settingsProvider = settingsProvider;
    }

    public override string SchemeName => "WERO";
    private WeroApiSettings Settings => _settingsProvider.Current.Wero;
    private PspApiClient Api => new(HttpClient, Settings, SchemeName, Logger);

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        PspStrategyHelper.EnsureProductionReady(Settings, SchemeName);
        var (debtorIban, creditorIban) = Settings.ResolveAccounts(transaction.MerchantId);

        var instaPayRequest = new
        {
            messageType = "InstaPayRequest",
            debtorAccount = new { iban = debtorIban },
            creditorAccount = new { iban = creditorIban },
            instructedAmount = new { currency = transaction.Currency, amount = transaction.Amount },
            remittanceInformation = transaction.EndToEndId,
            requestedExecutionDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            clearingSystem = "SEPA_INST",
            callbackUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
        };

        try
        {
            var response = await Api.PostAsync(Settings.InstantPaymentPath, instaPayRequest, ct);
            var externalRef = PspStrategyHelper.ReadString(response, "paymentId", "endToEndId")
                              ?? transaction.EndToEndId;
            var redirectUrl = PspStrategyHelper.ReadString(response, "redirectUrl", "authorizationUrl");

            Logger.LogInformation("Wero A2A ref={Ref}", externalRef);
            return new PaymentResult(true, externalRef, "Pending", redirectUrl, JsonSerializer.Serialize(instaPayRequest));
        }
        catch (PspIntegrationException ex) when (Settings.EnableSimulationFallback)
        {
            return PspStrategyHelper.BuildFallback(
                transaction, SchemeName, "Pending", "https://pay.wero.eu/confirm", instaPayRequest, null, Logger, ex);
        }
    }

    public override Task<WebhookResult> HandleWebhookAsync(
        string payload, Dictionary<string, string> headers, CancellationToken ct) =>
        PspWebhookProcessor.ProcessAsync(
            SchemeName, payload, Repository, Logger,
            root => PspJson.ReadString(root, "paymentId", "endToEndId"), ct);

    public override async Task<RefundResult> RefundAsync(Guid transactionId, decimal amount, CancellationToken ct)
    {
        var transaction = await Repository.GetByIdAsync(transactionId, ct);
        if (transaction is null)
            return new RefundResult(false, string.Empty, amount, "NOT_FOUND");

        if (string.IsNullOrWhiteSpace(transaction.ExternalReference))
            return await base.RefundAsync(transactionId, amount, ct);

        var path = Settings.RefundPath.Replace("{paymentId}", transaction.ExternalReference, StringComparison.Ordinal);
        try
        {
            var response = await Api.PostAsync(path, new { amount = new { currency = transaction.Currency, value = amount } }, ct);
            var refundId = PspStrategyHelper.ReadString(response, "refundId", "id") ?? $"RF-WRO-{Guid.NewGuid():N}"[..24];
            return new RefundResult(true, refundId, amount, "REFUNDED");
        }
        catch (PspIntegrationException ex) when (Settings.EnableSimulationFallback)
        {
            Logger.LogWarning(ex, "Reembolso Wero em fallback simulado");
            return await base.RefundAsync(transactionId, amount, ct);
        }
    }
}
