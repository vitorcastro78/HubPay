using MediatR;

namespace HubPay.Application.Commands;

public sealed record RefundPaymentCommand(Guid TransactionId, decimal? Amount) : IRequest<RefundPaymentResult>;

public sealed record RefundPaymentResult(bool Success, string RefundReference, string Status);
