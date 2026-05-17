using HubPay.Application.DTOs;
using HubPay.Application.Queries;
using HubPay.Domain.Enums;
using HubPay.Domain.Interfaces;
using MediatR;

namespace HubPay.Application.Handlers;

public sealed class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly ITransactionRepository _repository;

    public GetDashboardStatsQueryHandler(ITransactionRepository repository) => _repository = repository;

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var totalVolume = await _repository.GetTotalVolumeAsync(ct);
        var authorized = await _repository.CountByStatusAsync(TransactionStatus.Authorized, ct);
        var settled = await _repository.CountByStatusAsync(TransactionStatus.Settled, ct);
        var pending = await _repository.CountByStatusAsync(TransactionStatus.Pending, ct);
        var blocked = await _repository.CountByStatusAsync(TransactionStatus.BlockedByAntiFraud, ct);
        var failed = await _repository.CountByStatusAsync(TransactionStatus.Failed, ct);
        var tra = await _repository.CountTraExemptionsAsync(ct);

        var totalCompleted = authorized + settled;
        var totalAttempts = totalCompleted + pending + blocked + failed;
        var conversion = totalAttempts > 0 ? (decimal)totalCompleted / totalAttempts * 100m : 0m;

        var (allTx, _) = await _repository.GetPagedAsync(1, 10000, ct);
        var weroCount = allTx.Count(t => t.PaymentScheme is "WERO");
        var cardSchemes = new[] { "EURO6000", "CARTESBANCAIRES" };
        var cardCount = allTx.Count(t => cardSchemes.Contains(t.PaymentScheme));

        return new DashboardStatsDto(totalVolume, conversion, blocked, tra, weroCount, cardCount);
    }
}
