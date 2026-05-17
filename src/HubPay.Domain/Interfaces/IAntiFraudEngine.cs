using HubPay.Domain.Entities;
using HubPay.Domain.Models;

namespace HubPay.Domain.Interfaces;

public interface IAntiFraudEngine
{
    Task<AntiFraudEvaluationResult> EvaluateAsync(Transaction transaction, CancellationToken ct = default);
}
