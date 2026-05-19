using MediatR;

namespace HubPay.Application.Commands;

public sealed record RefundPaymentCommand(Guid TransactionId, decimal? Amount) : IRequest<RefundPaymentResult>;

/// <summary>Outcome of a refund request.</summary>
/// <param name="Success">Whether the PSP accepted the refund.</param>
/// <param name="RefundReference">PSP refund reference.</param>
/// <param name="Status">Updated transaction or refund status.</param>
public sealed record RefundPaymentResult(bool Success, string RefundReference, string Status);
