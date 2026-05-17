namespace HubPay.Domain.Models;

public sealed record PaymentResult(
    bool Success,
    string ExternalReference,
    string Status,
    string? RedirectUrl,
    string? PayloadJson);
