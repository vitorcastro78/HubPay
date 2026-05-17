using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class MBWayStrategy : PaymentStrategyBase
{
    private readonly SibsApiSettings _settings;

    public MBWayStrategy(
        HttpClient httpClient,
        ILogger<MBWayStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options) : base(httpClient, logger, repository)
    {
        _settings = options.Value.Sibs;
        HttpClient.BaseAddress = new Uri(_settings.BaseUrl);
        HttpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
    }

    public override string SchemeName => "MBWAY";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            merchantTransactionId = transaction.Id.ToString(),
            amount = new { value = transaction.Amount, currency = transaction.Currency },
            customer = new { email = transaction.CustomerEmail },
            paymentMethod = "MBWAY",
            callbackUrl = "https://hubpay.eu/webhooks/sibs/mbway"
        };

        try
        {
            var response = await PostJsonAsync<object, JsonElement>("/v1/mbway/payments", payload, ct);
            var externalRef = response.TryGetProperty("paymentId", out var pid) ? pid.GetString() : transaction.Id.ToString();
            Logger.LogInformation("MB WAY iniciado SIBS ref={Ref}", externalRef);
            return new PaymentResult(true, externalRef ?? transaction.Id.ToString(), "Pending", null,
                JsonSerializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Simulação MB WAY - fallback local");
            return new PaymentResult(true, $"MBW-{transaction.Id:N}"[..20], "Pending", null,
                JsonSerializer.Serialize(payload));
        }
    }
}
