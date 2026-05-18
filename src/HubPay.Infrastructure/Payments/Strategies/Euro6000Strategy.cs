using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class Euro6000Strategy : PspEndpointStrategyBase
{
    public Euro6000Strategy(
        HttpClient httpClient,
        IHubPaySettingsProvider settingsProvider,
        ILogger<Euro6000Strategy> logger,
        ITransactionRepository repository)
        : base(httpClient, settingsProvider, s => s.Euro6000, "EURO6000", logger, repository) { }

    protected override string PaymentInitPath => "/v1/debit/route";
    protected override string? DefaultRedirectUrl => null;

    protected override object BuildPaymentPayload(Transaction transaction) => new
    {
        merchantId = transaction.MerchantId,
        amount = new { value = transaction.Amount, currency = transaction.Currency },
        cardScheme = "EURO6000",
        panToken = $"tok_{transaction.DeviceFingerprint[..Math.Min(8, transaction.DeviceFingerprint.Length)]}",
        notificationUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
    };
}
