using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BancontactStrategy : PspEndpointStrategyBase
{
    public BancontactStrategy(
        HttpClient httpClient,
        ILogger<BancontactStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options)
        : base(httpClient, options.Value.Bancontact, "BANCONTACT", logger, repository) { }

    public override string SchemeName => "BANCONTACT";
    protected override string PaymentInitPath => "/v1/payments";
    protected override string? DefaultRedirectUrl => null;

    protected override object BuildPaymentPayload(Transaction transaction) => new
    {
        flow = "HYBRID_APP_WEB",
        amount = new { value = transaction.Amount, currency = "EUR" },
        deepLink = $"bancontact://pay/{transaction.Id}",
        webRedirect = $"https://pay.bancontact.be/{transaction.Id}",
        notificationUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
    };
}
