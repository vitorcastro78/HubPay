using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class Euro6000Strategy : PspEndpointStrategyBase
{
    public Euro6000Strategy(
        HttpClient httpClient,
        ILogger<Euro6000Strategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options)
        : base(httpClient, options.Value.Euro6000, "EURO6000", logger, repository) { }

    public override string SchemeName => "EURO6000";
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
