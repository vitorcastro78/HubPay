using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class VippsMobilePayStrategy : PspEndpointStrategyBase
{
    public VippsMobilePayStrategy(
        HttpClient httpClient,
        IHubPaySettingsProvider settingsProvider,
        ILogger<VippsMobilePayStrategy> logger,
        ITransactionRepository repository)
        : base(httpClient, settingsProvider, s => s.VippsMobilePay, "VIPPSMOBILEPAY", logger, repository) { }

    protected override string PaymentInitPath => "/epayment/v1/payments";
    protected override string? DefaultRedirectUrl => "https://api.vipps.no/dwo/checkout";

    protected override object BuildPaymentPayload(Transaction transaction) => new
    {
        merchantSerialNumber = transaction.MerchantId,
        amount = new { value = transaction.Amount * 100, currency = "NOK" },
        settlementScheme = "SEPA_INST",
        endToEndId = transaction.EndToEndId,
        callbackUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
    };
}
