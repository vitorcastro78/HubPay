namespace HubPay.Domain.Interfaces;

public interface ICamt053StatementReader
{
    Task<IReadOnlyList<Camt053Statement>> ReadPendingStatementsAsync(CancellationToken ct = default);
    Task MarkAsProcessedAsync(string fileName, CancellationToken ct = default);
}

public sealed record Camt053Statement(string FileName, string XmlContent);

public sealed record Camt053Entry(string EndToEndId, decimal Amount, string Currency);
