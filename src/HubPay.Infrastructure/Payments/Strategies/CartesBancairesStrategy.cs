using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class CartesBancairesStrategy : PspEndpointStrategyBase
{
    public CartesBancairesStrategy(
        HttpClient httpClient,
        ILogger<CartesBancairesStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options)
        : base(httpClient, options.Value.CartesBancaires, "CARTESBANCAIRES", logger, repository) { }

    public override string SchemeName => "CARTESBANCAIRES";
    protected override string PaymentInitPath => "/v1/payments/authorize";
    protected override string? DefaultRedirectUrl => "https://acs.cartesbancaires.fr/challenge";

    protected override object BuildPaymentPayload(Transaction transaction) => new
    {
        scheme = "CB",
        amount = new { value = transaction.Amount, currency = "EUR" },
        merchantId = transaction.MerchantId,
        authentication = "3DS",
        notificationUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
    };
}
