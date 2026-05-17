namespace HubPay.Domain.Models;

public sealed record RefundResult(
    bool Success,
    string RefundReference,
    decimal RefundedAmount,
    string Status);
