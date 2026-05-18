using System.Diagnostics;
using HubPay.Infrastructure.Telemetry;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using HubPay.Infrastructure.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace HubPay.Infrastructure.AntiFraud;

public sealed class AntiFraudEngine : IAntiFraudEngine, IDisposable
{
    private readonly RedisFeatureStore _featureStore;
    private readonly IAntiFraudAuditStore _auditStore;
    private readonly ILogger<AntiFraudEngine> _logger;
    private readonly HubPaySettings _settings;
    private readonly InferenceSession? _session;
    private readonly ResiliencePipeline<decimal> _inferencePipeline;

    public AntiFraudEngine(
        RedisFeatureStore featureStore,
        IAntiFraudAuditStore auditStore,
        IOptions<HubPaySettings> options,
        ILogger<AntiFraudEngine> logger)
    {
        _featureStore = featureStore;
        _auditStore = auditStore;
        _logger = logger;
        _settings = options.Value;

        if (File.Exists(_settings.OnnxModelPath))
        {
            _session = new InferenceSession(_settings.OnnxModelPath);
        }
        else
        {
            _logger.LogWarning("Modelo ONNX não encontrado em {Path}. Será usado motor matemático local.", _settings.OnnxModelPath);
        }

        _inferencePipeline = new ResiliencePipelineBuilder<decimal>()
            .AddTimeout(TimeSpan.FromMilliseconds(12))
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<decimal>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(60)
            })
            .Build();
    }

    public async Task<AntiFraudEvaluationResult> EvaluateAsync(Transaction transaction, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var countryCode = transaction.CountryCode ?? ExtractCountryFromIp(transaction.CustomerIP);

        var deviceTask = _featureStore.GetDeviceTransactionCountAsync(transaction.DeviceFingerprint, TimeSpan.FromMinutes(5), ct);
        var emailCountriesTask = _featureStore.GetEmailCountryCountAsync(transaction.CustomerEmail, ct);
        await Task.WhenAll(deviceTask, emailCountriesTask);
        await _featureStore.IncrementDeviceCounterAsync(transaction.DeviceFingerprint, ct);
        if (!string.IsNullOrEmpty(countryCode))
            await _featureStore.RecordEmailCountryAsync(transaction.CustomerEmail, countryCode, ct);

        var deviceCount = await deviceTask;
        var emailCountries = await emailCountriesTask;
        var normalizedAmount = (float)Math.Min((double)transaction.Amount / 1000.0, 1.0);
        var ipHash = (float)(transaction.CustomerIP.GetHashCode() & 0x7FFFFFFF) / int.MaxValue;

        var features = new AntiFraudInputFeatures(
            transaction.Amount,
            transaction.CustomerIP,
            deviceCount,
            emailCountries,
            normalizedAmount,
            ipHash);

        bool usedFallback = false;
        decimal score;

        try
        {
            score = await _inferencePipeline.ExecuteAsync(async token =>
            {
                token.ThrowIfCancellationRequested();
                return await Task.Run(() => RunInference(features), token);
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback anti-fraude ativado (ONNX/timeout/circuit breaker)");
            usedFallback = true;
            score = RunFallbackRules(transaction.Amount, deviceCount, emailCountries);
        }

        sw.Stop();
        HubPayMetrics.AntiFraudLatencyMs.Record(sw.Elapsed.TotalMilliseconds);
        var scaStatus = ResolveScaStatus(score);
        var result = new AntiFraudEvaluationResult(score, scaStatus, sw.ElapsedMilliseconds, features, usedFallback);
        await _auditStore.SaveEvaluationAsync(transaction.Id, result, ct);
        return result;
    }

    private decimal RunInference(AntiFraudInputFeatures features)
    {
        if (_session is not null)
        {
            var input = new DenseTensor<float>(new[] { 1, 4 });
            input[0, 0] = features.NormalizedAmount;
            input[0, 1] = features.IpHashFeature;
            input[0, 2] = features.DeviceTransactionsLast5Min / 10f;
            input[0, 3] = features.EmailCountriesLastHour / 5f;

            var inputName = _session.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, input)
            };

            using var results = _session.Run(inputs);
            var outputTensor = results.First().AsEnumerable<float>().ToArray();
            var output = outputTensor.Length > 0 ? outputTensor[0] : 0f;
            var normalized = output is >= 0f and <= 1f ? output : output / 100f;
            return Math.Clamp((decimal)normalized * 100m, 0m, 100m);
        }

        return RunMathematicalModel(features);
    }

    private static decimal RunMathematicalModel(AntiFraudInputFeatures features)
    {
        var score = features.NormalizedAmount * 25f
                    + features.DeviceTransactionsLast5Min * 8f
                    + features.EmailCountriesLastHour * 12f
                    + features.IpHashFeature * 15f;
        return Math.Clamp((decimal)score, 0m, 100m);
    }

    private static decimal RunFallbackRules(decimal amount, int deviceCount, int emailCountries)
    {
        if (amount < 30m && deviceCount <= 2 && emailCountries <= 1)
            return 10m;
        return 45m;
    }

    private static string ResolveScaStatus(decimal score)
    {
        if (score < 15m) return "TRA";
        if (score <= 70m) return "SCA_REQUIRED";
        return "BLOCKED";
    }

    private static string ExtractCountryFromIp(string ip) =>
        ip.StartsWith("192.168.") ? "PT" : "EU";

    public void Dispose() => _session?.Dispose();
}
