namespace HubPay.Domain.Models;

public sealed record WebhookResult(
    bool Processed,
    Guid? TransactionId,
    string NewStatus,
    string? PayloadJson);
