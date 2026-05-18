using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BancontactStrategy : PspEndpointStrategyBase
{
    public BancontactStrategy(
        HttpClient httpClient,
        IHubPaySettingsProvider settingsProvider,
        ILogger<BancontactStrategy> logger,
        ITransactionRepository repository)
        : base(httpClient, settingsProvider, s => s.Bancontact, "BANCONTACT", logger, repository) { }

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
