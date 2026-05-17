using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class IDealStrategy : PaymentStrategyBase
{
    public IDealStrategy(HttpClient httpClient, ILogger<IDealStrategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository)
    {
        HttpClient.BaseAddress = new Uri("https://api.ideal.nl/sandbox");
    }

    public override string SchemeName => "IDEAL";

    public override Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var transactionToken = Guid.NewGuid().ToString("N");
        var redirectUrl = $"https://ideal.nl/checkout?token={transactionToken}";
        var qrPayload = $"ideal://pay?token={transactionToken}&amount={transaction.Amount:F2}&currency=EUR";

        var payload = new { transactionToken, redirectUrl, qrCode = qrPayload, endToEndId = transaction.EndToEndId };
        return Task.FromResult(new PaymentResult(true, transactionToken, "Pending", redirectUrl, JsonSerializer.Serialize(payload)));
    }
}
