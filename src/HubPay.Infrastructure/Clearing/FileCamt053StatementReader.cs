using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Clearing;

public sealed class FileCamt053StatementReader : ICamt053StatementReader
{
    private readonly Camt053Settings _settings;
    private readonly ILogger<FileCamt053StatementReader> _logger;

    public FileCamt053StatementReader(IOptions<HubPaySettings> options, ILogger<FileCamt053StatementReader> logger)
    {
        _settings = options.Value.Camt053;
        _logger = logger;
        Directory.CreateDirectory(_settings.InboundDirectory);
        Directory.CreateDirectory(_settings.ProcessedDirectory);
    }

    public async Task<IReadOnlyList<Camt053Statement>> ReadPendingStatementsAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_settings.InboundDirectory, "*.xml", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();

        var statements = new List<Camt053Statement>();
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file, ct);
            statements.Add(new Camt053Statement(Path.GetFileName(file), content));
            _logger.LogInformation("Extrato camt.053 lido: {File}", file);
        }

        return statements;
    }

    public Task MarkAsProcessedAsync(string fileName, CancellationToken ct = default)
    {
        var source = Path.Combine(_settings.InboundDirectory, fileName);
        if (!File.Exists(source)) return Task.CompletedTask;

        var destination = Path.Combine(_settings.ProcessedDirectory, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}");
        File.Move(source, destination, overwrite: true);
        return Task.CompletedTask;
    }
}
