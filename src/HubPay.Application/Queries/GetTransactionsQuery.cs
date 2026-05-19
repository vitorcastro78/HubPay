using HubPay.Application.DTOs;
using MediatR;

namespace HubPay.Application.Queries;

public sealed record GetTransactionsQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<TransactionDto>>;

/// <summary>Paginated API result wrapper.</summary>
/// <param name="Items">Page items.</param>
/// <param name="Total">Total matching rows.</param>
/// <param name="Page">Current page (1-based).</param>
/// <param name="PageSize">Page size.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
