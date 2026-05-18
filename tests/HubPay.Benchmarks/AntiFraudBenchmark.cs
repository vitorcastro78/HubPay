using BenchmarkDotNet.Attributes;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.AntiFraud;
using HubPay.Infrastructure.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HubPay.Benchmarks;

[MemoryDiagnoser]
public sealed class AntiFraudBenchmark
{
    private IAntiFraudEngine _engine = null!;
    private Transaction _transaction = null!;

    [GlobalSetup]
    public void Setup()
    {
        var redis = ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
        var settings = Options.Create(new HubPaySettings
        {
            OnnxModelPath = Path.Combine(AppContext.BaseDirectory, "Models", "antifraud.onnx")
        });

        _engine = new AntiFraudEngine(
            new RedisFeatureStore(redis),
            new RedisAntiFraudAuditStore(redis),
            settings,
            NullLogger<AntiFraudEngine>.Instance);

        _transaction = Transaction.Create(
            "BENCH", 45m, "EUR", "MBWAY", $"E2E-BENCH-{Guid.NewGuid():N}", "10.0.0.1", "fp-bench", "bench@test.com");
    }

    [Benchmark]
    public async Task EvaluateAsync()
    {
        _transaction = Transaction.Create(
            "BENCH", 45m, "EUR", "MBWAY", $"E2E-BENCH-{Guid.NewGuid():N}", "10.0.0.1", "fp-bench", "bench@test.com");
        await _engine.EvaluateAsync(_transaction);
    }
}
