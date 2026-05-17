using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class VippsMobilePayStrategy : PaymentStrategyBase
{
    public VippsMobilePayStrategy(HttpClient httpClient, ILogger<VippsMobilePayStrategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository)
    {
        HttpClient.BaseAddress = new Uri("https://api.vipps.no/sandbox");
    }

    public override string SchemeName => "VIPPSMOBILEPAY";

    public override Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var payload = new
        {
            merchantSerialNumber = transaction.MerchantId,
            amount = transaction.Amount * 100,
            currency = "NOK",
            settlementScheme = "SEPA_INST",
            endToEndId = transaction.EndToEndId
        };

        return Task.FromResult(new PaymentResult(
            true, $"VIP-{transaction.Id:N}"[..20], "Pending",
            "https://api.vipps.no/dwo/checkout", JsonSerializer.Serialize(payload)));
    }
}
