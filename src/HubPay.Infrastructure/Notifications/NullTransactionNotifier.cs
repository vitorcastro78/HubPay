using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;

namespace HubPay.Infrastructure.Notifications;

public sealed class NullTransactionNotifier : ITransactionNotifier
{
    public Task NotifyUpdatedAsync(Transaction transaction, CancellationToken ct = default) =>
        Task.CompletedTask;
}
