using HubPay.Domain.Entities;
using HubPay.Domain.Models;

namespace HubPay.Domain.Interfaces;

public interface IPaymentStrategy
{
    string SchemeName { get; }
    Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct);
    Task<WebhookResult> HandleWebhookAsync(string payload, Dictionary<string, string> headers, CancellationToken ct);
    Task<RefundResult> RefundAsync(Guid transactionId, decimal amount, CancellationToken ct);
}
