using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class VippsMobilePayStrategy : PspEndpointStrategyBase
{
    public VippsMobilePayStrategy(
        HttpClient httpClient,
        ILogger<VippsMobilePayStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options)
        : base(httpClient, options.Value.VippsMobilePay, "VIPPSMOBILEPAY", logger, repository) { }

    public override string SchemeName => "VIPPSMOBILEPAY";
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
