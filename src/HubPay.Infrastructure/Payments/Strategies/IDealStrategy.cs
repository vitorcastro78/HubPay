using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class IDealStrategy : PspEndpointStrategyBase
{
    public IDealStrategy(
        HttpClient httpClient,
        ILogger<IDealStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options)
        : base(httpClient, options.Value.Ideal, "IDEAL", logger, repository) { }

    public override string SchemeName => "IDEAL";
    protected override string PaymentInitPath => "/v1/transactions";
    protected override string? DefaultRedirectUrl => "https://ideal.nl/checkout";

    protected override object BuildPaymentPayload(Transaction transaction)
    {
        var token = Guid.NewGuid().ToString("N");
        return new
        {
            transactionToken = token,
            amount = new { value = transaction.Amount, currency = "EUR" },
            endToEndId = transaction.EndToEndId,
            returnUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
        };
    }
}
