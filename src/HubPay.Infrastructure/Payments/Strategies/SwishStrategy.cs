using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class SwishStrategy : PspEndpointStrategyBase
{
    public SwishStrategy(
        HttpClient httpClient,
        IHubPaySettingsProvider settingsProvider,
        ILogger<SwishStrategy> logger,
        ITransactionRepository repository)
        : base(httpClient, settingsProvider, s => s.Swish, "SWISH", logger, repository) { }

    protected override string PaymentInitPath => "/v2/paymentrequests";
    protected override string? DefaultRedirectUrl => "swish://payment";

    protected override object BuildPaymentPayload(Transaction transaction) => new
    {
        payeeAlias = transaction.MerchantId,
        amount = transaction.Amount,
        currency = "SEK",
        settlement = "SEPA_INST",
        message = transaction.EndToEndId,
        callbackUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
    };
}
