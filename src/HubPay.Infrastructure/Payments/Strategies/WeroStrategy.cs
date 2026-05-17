using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class WeroStrategy : PaymentStrategyBase
{
    public WeroStrategy(
        HttpClient httpClient,
        ILogger<WeroStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options) : base(httpClient, logger, repository)
    {
        var settings = options.Value.Wero;
        HttpClient.BaseAddress = new Uri(settings.BaseUrl);
        HttpClient.DefaultRequestHeaders.Add("X-API-Key", settings.ApiKey);
    }

    public override string SchemeName => "WERO";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var instaPayRequest = new
        {
            messageType = "InstaPayRequest",
            debtorAccount = new { iban = "DE89370400440532013000" },
            creditorAccount = new { iban = "FR1420041010050500013M02606" },
            instructedAmount = new { currency = "EUR", amount = transaction.Amount },
            remittanceInformation = transaction.EndToEndId,
            requestedExecutionDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            clearingSystem = "SEPA_INST"
        };

        try
        {
            await PostJsonAsync<object, JsonElement>("/v1/instant-payments", instaPayRequest, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Wero A2A simulação");
        }

        return new PaymentResult(true, $"WRO-{transaction.EndToEndId}"[..24], "Pending",
            "https://pay.wero.eu/confirm", JsonSerializer.Serialize(instaPayRequest));
    }
}
