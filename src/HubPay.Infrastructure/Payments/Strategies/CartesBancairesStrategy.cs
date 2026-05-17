using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class CartesBancairesStrategy : PaymentStrategyBase
{
    public CartesBancairesStrategy(HttpClient httpClient, ILogger<CartesBancairesStrategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository)
    {
        HttpClient.BaseAddress = new Uri("https://api.cartesbancaires.fr/sandbox");
    }

    public override string SchemeName => "CARTESBANCAIRES";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            scheme = "CB",
            amount = transaction.Amount,
            currency = "EUR",
            merchantId = transaction.MerchantId,
            authentication = "3DS"
        };

        try
        {
            await PostJsonAsync<object, JsonElement>("/payments/authorize", payload, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Cartes Bancaires simulação");
        }

        return new PaymentResult(true, $"CB-{transaction.Id:N}"[..20], "Pending",
            "https://acs.cartesbancaires.fr/challenge", JsonSerializer.Serialize(payload));
    }
}
