using HubPay.Domain.Entities;
using HubPay.Domain.Enums;

namespace HubPay.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Transaction?> GetByEndToEndIdAsync(string endToEndId, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetByStatusAsync(TransactionStatus status, CancellationToken ct = default);
    Task<(IReadOnlyList<Transaction> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task UpdateAsync(Transaction transaction, CancellationToken ct = default);
    Task<decimal> GetTotalVolumeAsync(CancellationToken ct = default);
    Task<int> CountByStatusAsync(TransactionStatus status, CancellationToken ct = default);
    Task<int> CountTraExemptionsAsync(CancellationToken ct = default);
}
