using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BancomatPayStrategy : PspEndpointStrategyBase
{
    public BancomatPayStrategy(
        HttpClient httpClient,
        ILogger<BancomatPayStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options)
        : base(httpClient, options.Value.BancomatPay, "BANCOMATPAY", logger, repository) { }

    public override string SchemeName => "BANCOMATPAY";
    protected override string PaymentInitPath => "/v1/payments";
    protected override string? DefaultRedirectUrl => null;

    protected override object BuildPaymentPayload(Transaction transaction) => new
    {
        digitalId = transaction.CustomerEmail,
        amount = new { value = transaction.Amount, currency = "EUR" },
        notificationUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
    };
}
