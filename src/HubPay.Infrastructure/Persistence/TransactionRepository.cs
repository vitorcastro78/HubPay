using HubPay.Domain.Entities;
using HubPay.Domain.Enums;
using HubPay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HubPay.Infrastructure.Persistence;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly HubPayDbContext _db;

    public TransactionRepository(HubPayDbContext db) => _db = db;

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Transaction?> GetByEndToEndIdAsync(string endToEndId, CancellationToken ct = default) =>
        await _db.Transactions.FirstOrDefaultAsync(t => t.EndToEndId == endToEndId, ct);

    public async Task<Transaction?> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default) =>
        await _db.Transactions.FirstOrDefaultAsync(t => t.ExternalReference == externalReference, ct);

    public async Task<IReadOnlyList<Transaction>> GetByStatusAsync(TransactionStatus status, CancellationToken ct = default) =>
        await _db.Transactions.Where(t => t.Status == status).ToListAsync(ct);

    public async Task<(IReadOnlyList<Transaction> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsNoTracking().OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        await _db.Transactions.AddAsync(transaction, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<decimal> GetTotalVolumeAsync(CancellationToken ct = default) =>
        await _db.Transactions
            .Where(t => t.Status == TransactionStatus.Settled || t.Status == TransactionStatus.Authorized)
            .SumAsync(t => t.Amount, ct);

    public async Task<int> CountByStatusAsync(TransactionStatus status, CancellationToken ct = default) =>
        await _db.Transactions.CountAsync(t => t.Status == status, ct);

    public async Task<int> CountTraExemptionsAsync(CancellationToken ct = default) =>
        await _db.Transactions.CountAsync(t => t.ScaStatus == "TRA", ct);
}
