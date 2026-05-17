using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class MultibancoStrategy : PaymentStrategyBase
{
    public MultibancoStrategy(HttpClient httpClient, ILogger<MultibancoStrategy> logger, ITransactionRepository repository)
        : base(httpClient, logger, repository) { }

    public override string SchemeName => "MULTIBANCO";

    public override Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        var (entity, reference, dueDate) = MultibancoReferenceGenerator.Generate(transaction.Amount, transaction.MerchantId);
        var payload = new
        {
            entity,
            reference,
            amount = transaction.Amount,
            currency = transaction.Currency,
            dueDate = dueDate.ToString("yyyy-MM-dd"),
            endToEndId = transaction.EndToEndId
        };

        Logger.LogInformation("Multibanco gerado Entidade={Entity} Ref={Ref}", entity, reference);
        return Task.FromResult(new PaymentResult(
            true,
            $"{entity}/{reference}",
            "Pending",
            null,
            JsonSerializer.Serialize(payload)));
    }
}
