using HubPay.Domain.Entities;

namespace HubPay.Domain.Interfaces;

public interface ITransactionNotifier
{
    Task NotifyUpdatedAsync(Transaction transaction, CancellationToken ct = default);
}
