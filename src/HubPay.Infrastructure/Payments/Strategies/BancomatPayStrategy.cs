using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BancomatPayStrategy : PaymentStrategyBase
{
    public BancomatPayStrategy(HttpClient httpClient, ILogger<BancomatPayStrategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository)
    {
        HttpClient.BaseAddress = new Uri("https://api.bancomatpay.it/sandbox");
    }

    public override string SchemeName => "BANCOMATPAY";

    public override Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            digitalId = transaction.CustomerEmail,
            phone = "+393331234567",
            amount = transaction.Amount,
            currency = "EUR"
        };

        return Task.FromResult(new PaymentResult(
            true, $"BCP-{transaction.Id:N}"[..20], "Pending", null, JsonSerializer.Serialize(payload)));
    }
}
