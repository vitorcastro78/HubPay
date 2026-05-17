using HubPay.Application.DTOs;
using MediatR;

namespace HubPay.Application.Queries;

public sealed record GetAntiFraudDetailQuery(Guid TransactionId) : IRequest<AntiFraudDetailDto?>;
