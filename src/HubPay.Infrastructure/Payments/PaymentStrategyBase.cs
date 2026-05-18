using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments;

public abstract class PaymentStrategyBase : IPaymentStrategy
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly ITransactionRepository Repository;

    protected PaymentStrategyBase(HttpClient httpClient, ILogger logger, ITransactionRepository repository)
    {
        HttpClient = httpClient;
        Logger = logger;
        Repository = repository;
    }

    public abstract string SchemeName { get; }

    public abstract Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct);

    public abstract Task<WebhookResult> HandleWebhookAsync(string payload, Dictionary<string, string> headers, CancellationToken ct);

    public virtual async Task<RefundResult> RefundAsync(Guid transactionId, decimal amount, CancellationToken ct)
    {
        var transaction = await Repository.GetByIdAsync(transactionId, ct);
        if (transaction is null)
            return new RefundResult(false, string.Empty, amount, "NOT_FOUND");

        var refundRef = $"RF-{SchemeName}-{Guid.NewGuid():N}"[..24];
        Logger.LogInformation("Reembolso {Scheme} tx={TxId} amount={Amount}", SchemeName, transactionId, amount);
        return new RefundResult(true, refundRef, amount, "REFUNDED");
    }
}
