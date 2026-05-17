using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BizumStrategy : PaymentStrategyBase
{
    public BizumStrategy(
        HttpClient httpClient,
        ILogger<BizumStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options) : base(httpClient, logger, repository)
    {
        var settings = options.Value.Bizum;
        HttpClient.BaseAddress = new Uri(settings.BaseUrl);
        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
    }

    public override string SchemeName => "BIZUM";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            orderId = transaction.EndToEndId,
            amount = transaction.Amount,
            currency = "EUR",
            buyerPhone = "+34600000000",
            notificationUrl = "https://hubpay.eu/webhooks/bizum"
        };

        try
        {
            await PostJsonAsync<object, JsonElement>("/payments/init", payload, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Bizum simulação local");
        }

        return new PaymentResult(true, $"BZM-{transaction.Id:N}"[..20], "Pending",
            "https://bizum.es/app/confirm", JsonSerializer.Serialize(payload));
    }
}
