using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Enums;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.Payments.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;

namespace HubPay.Tests.Unit.Payments;

public sealed class PspWebhookProcessorTests
{
    [Fact]
    public async Task ProcessAsync_AuthorizesByMerchantTransactionId()
    {
        var txId = Guid.NewGuid();
        var repo = new FakeTransactionRepository(txId);
        var payload = JsonSerializer.Serialize(new
        {
            merchantTransactionId = txId.ToString(),
            paymentId = "PSP-123",
            status = "SUCCESS"
        });

        var result = await PspWebhookProcessor.ProcessAsync(
            "MBWAY",
            payload,
            repo,
            NullLogger.Instance,
            root => root.GetProperty("paymentId").GetString(),
            CancellationToken.None);

        Assert.True(result.Processed);
        Assert.Equal("Authorized", result.NewStatus);
        Assert.Equal(TransactionStatus.Authorized, repo.Transaction!.Status);
    }

    [Fact]
    public async Task ProcessAsync_ResolvesByExternalReference()
    {
        var txId = Guid.NewGuid();
        var repo = new FakeTransactionRepository(txId, externalRef: "PSP-EXT-1");
        var payload = JsonSerializer.Serialize(new { paymentId = "PSP-EXT-1", status = "PAID" });

        var result = await PspWebhookProcessor.ProcessAsync(
            "BIZUM",
            payload,
            repo,
            NullLogger.Instance,
            root => root.GetProperty("paymentId").GetString(),
            CancellationToken.None);

        Assert.True(result.Processed);
        Assert.Equal(TransactionStatus.Authorized, repo.Transaction!.Status);
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public Transaction? Transaction { get; private set; }

        public FakeTransactionRepository(Guid id, string? externalRef = null)
        {
            Transaction = Transaction.Create("M1", 10m, "EUR", "MBWAY", "E2E-1", "1.1.1.1", "fp", "a@b.com", "+351912345678");
            typeof(Transaction).GetProperty(nameof(Transaction.Id))!
                .SetValue(Transaction, id);
            Transaction.MarkPending(externalRef ?? "pending");
        }

        public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Transaction?.Id == id ? Transaction : null);

        public Task<Transaction?> GetByEndToEndIdAsync(string endToEndId, CancellationToken ct = default) =>
            Task.FromResult<Transaction?>(null);

        public Task<Transaction?> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default) =>
            Task.FromResult(Transaction?.ExternalReference == externalReference ? Transaction : null);

        public Task<IReadOnlyList<Transaction>> GetByStatusAsync(TransactionStatus status, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>([]);

        public Task<(IReadOnlyList<Transaction> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult((Items: (IReadOnlyList<Transaction>)[], Total: 0));

        public Task AddAsync(Transaction transaction, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
        {
            Transaction = transaction;
            return Task.CompletedTask;
        }

        public Task<decimal> GetTotalVolumeAsync(CancellationToken ct = default) => Task.FromResult(0m);
        public Task<int> CountByStatusAsync(TransactionStatus status, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CountTraExemptionsAsync(CancellationToken ct = default) => Task.FromResult(0);
    }
}
