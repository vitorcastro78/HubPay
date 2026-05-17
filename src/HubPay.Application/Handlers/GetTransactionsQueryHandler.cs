using HubPay.Application.DTOs;
using HubPay.Application.Queries;
using HubPay.Domain.Interfaces;
using MediatR;

namespace HubPay.Application.Handlers;

public sealed class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsQueryHandler(ITransactionRepository repository) => _repository = repository;

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery query, CancellationToken ct)
    {
        var (items, total) = await _repository.GetPagedAsync(query.Page, query.PageSize, ct);
        var dtos = items.Select(t => new TransactionDto(
            t.Id,
            t.MerchantId,
            t.Amount,
            t.Currency,
            t.PaymentScheme,
            t.EndToEndId,
            t.Status.ToString(),
            t.ScaStatus,
            t.AntiFraudScore,
            t.CountryCode,
            t.CreatedAt,
            t.AntiFraudElapsedMs)).ToList();

        return new PagedResult<TransactionDto>(dtos, total, query.Page, query.PageSize);
    }
}
