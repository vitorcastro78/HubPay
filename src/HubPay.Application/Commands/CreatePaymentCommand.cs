using HubPay.Application.DTOs;
using MediatR;

namespace HubPay.Application.Commands;

public sealed record CreatePaymentCommand(CreatePaymentRequest Request) : IRequest<PaymentResponseDto>;
