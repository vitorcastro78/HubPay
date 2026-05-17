using HubPay.Application.DTOs;
using HubPay.Application.Queries;
using HubPay.Domain.Interfaces;
using MediatR;

namespace HubPay.Application.Handlers;

public sealed class GetAntiFraudDetailQueryHandler : IRequestHandler<GetAntiFraudDetailQuery, AntiFraudDetailDto?>
{
    private readonly ITransactionRepository _repository;
    private readonly IAntiFraudAuditStore _auditStore;

    public GetAntiFraudDetailQueryHandler(ITransactionRepository repository, IAntiFraudAuditStore auditStore)
    {
        _repository = repository;
        _auditStore = auditStore;
    }

    public async Task<AntiFraudDetailDto?> Handle(GetAntiFraudDetailQuery query, CancellationToken ct)
    {
        var transaction = await _repository.GetByIdAsync(query.TransactionId, ct);
        if (transaction is null) return null;

        var evaluation = await _auditStore.GetEvaluationAsync(transaction.Id, ct);
        if (evaluation is not null)
        {
            return new AntiFraudDetailDto(
                transaction.Id,
                evaluation.Features.Amount,
                evaluation.Features.CustomerIp,
                evaluation.Features.DeviceTransactionsLast5Min,
                evaluation.Features.EmailCountriesLastHour,
                evaluation.Features.NormalizedAmount,
                evaluation.Features.IpHashFeature,
                evaluation.Score,
                evaluation.ElapsedMilliseconds,
                evaluation.ScaStatus,
                transaction.Status.ToString(),
                evaluation.UsedFallback);
        }

        return new AntiFraudDetailDto(
            transaction.Id,
            transaction.Amount,
            transaction.CustomerIP,
            0,
            0,
            (float)Math.Min((double)transaction.Amount / 1000.0, 1.0),
            0f,
            transaction.AntiFraudScore,
            transaction.AntiFraudElapsedMs,
            transaction.ScaStatus,
            transaction.Status.ToString(),
            false);
    }
}
