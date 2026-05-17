using HubPay.Domain.Models;

namespace HubPay.Domain.Interfaces;

public interface IAntiFraudAuditStore
{
    Task SaveEvaluationAsync(Guid transactionId, AntiFraudEvaluationResult result, CancellationToken ct = default);
    Task<AntiFraudEvaluationResult?> GetEvaluationAsync(Guid transactionId, CancellationToken ct = default);
}
