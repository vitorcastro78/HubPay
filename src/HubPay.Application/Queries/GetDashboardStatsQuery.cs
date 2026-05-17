using HubPay.Application.DTOs;
using MediatR;

namespace HubPay.Application.Queries;

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
