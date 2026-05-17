using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class SwishStrategy : PaymentStrategyBase
{
    public SwishStrategy(HttpClient httpClient, ILogger<SwishStrategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository)
    {
        HttpClient.BaseAddress = new Uri("https://api.swish.nu/sandbox");
    }

    public override string SchemeName => "SWISH";

    public override Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            payeeAlias = transaction.MerchantId,
            amount = transaction.Amount,
            currency = "SEK",
            settlement = "SEPA_INST",
            message = transaction.EndToEndId
        };

        return Task.FromResult(new PaymentResult(
            true, $"SW-{transaction.Id:N}"[..20], "Pending", "swish://payment", JsonSerializer.Serialize(payload)));
    }
}
