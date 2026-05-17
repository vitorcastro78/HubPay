using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Models;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class Euro6000Strategy : PaymentStrategyBase
{
    public Euro6000Strategy(HttpClient httpClient, ILogger<Euro6000Strategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository)
    {
        HttpClient.BaseAddress = new Uri("https://api.euro6000.es/sandbox");
    }

    public override string SchemeName => "EURO6000";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            merchantId = transaction.MerchantId,
            amount = transaction.Amount,
            cardScheme = "EURO6000",
            panToken = $"tok_{transaction.DeviceFingerprint[..Math.Min(8, transaction.DeviceFingerprint.Length)]}"
        };

        try
        {
            await PostJsonAsync<object, JsonElement>("/debit/route", payload, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Euro6000 simulação");
        }

        return new PaymentResult(true, $"E6K-{transaction.Id:N}"[..20], "Pending", null, JsonSerializer.Serialize(payload));
    }
}
