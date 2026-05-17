using System.Text.Json;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using StackExchange.Redis;

namespace HubPay.Infrastructure.Redis;

public sealed class RedisAntiFraudAuditStore : IAntiFraudAuditStore
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisAntiFraudAuditStore(IConnectionMultiplexer redis) => _redis = redis;

    public async Task SaveEvaluationAsync(Guid transactionId, AntiFraudEvaluationResult result, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"hubpay:antifraud:detail:{transactionId}";
        var json = JsonSerializer.Serialize(result, JsonOptions);
        await db.StringSetAsync(key, json, TimeSpan.FromDays(7));
    }

    public async Task<AntiFraudEvaluationResult?> GetEvaluationAsync(Guid transactionId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"hubpay:antifraud:detail:{transactionId}";
        var value = await db.StringGetAsync(key);
        if (!value.HasValue) return null;
        return JsonSerializer.Deserialize<AntiFraudEvaluationResult>(value.ToString(), JsonOptions);
    }
}
