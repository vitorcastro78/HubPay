using HubPay.Application.DTOs;
using MediatR;

namespace HubPay.Application.Queries;

public sealed record GetTransactionsQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<TransactionDto>>;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
