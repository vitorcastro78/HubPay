using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BancontactStrategy : PaymentStrategyBase
{
    public BancontactStrategy(HttpClient httpClient, ILogger<BancontactStrategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository)
    {
        HttpClient.BaseAddress = new Uri("https://api.bancontact.be/sandbox");
    }

    public override string SchemeName => "BANCONTACT";

    public override Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            flow = "HYBRID_APP_WEB",
            amount = transaction.Amount,
            currency = "EUR",
            deepLink = $"bancontact://pay/{transaction.Id}",
            webRedirect = $"https://pay.bancontact.be/{transaction.Id}"
        };

        return Task.FromResult(new PaymentResult(
            true,
            $"BC-{transaction.Id:N}"[..20],
            "Pending",
            $"https://pay.bancontact.be/{transaction.Id}",
            JsonSerializer.Serialize(payload)));
    }
}
