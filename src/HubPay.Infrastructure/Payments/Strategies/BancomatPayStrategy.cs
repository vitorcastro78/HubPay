using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BancomatPayStrategy : PspEndpointStrategyBase
{
    public BancomatPayStrategy(
        HttpClient httpClient,
        IHubPaySettingsProvider settingsProvider,
        ILogger<BancomatPayStrategy> logger,
        ITransactionRepository repository)
        : base(httpClient, settingsProvider, s => s.BancomatPay, "BANCOMATPAY", logger, repository) { }

    protected override string PaymentInitPath => "/v1/payments";
    protected override string? DefaultRedirectUrl => null;

    protected override object BuildPaymentPayload(Transaction transaction)
    {
        var phone = PspPhoneValidator.RequirePhone(transaction.CustomerPhone, SchemeName);
        return new
        {
            digitalId = transaction.CustomerEmail,
            phone,
            amount = new { value = transaction.Amount, currency = "EUR" },
            notificationUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
        };
    }
}
