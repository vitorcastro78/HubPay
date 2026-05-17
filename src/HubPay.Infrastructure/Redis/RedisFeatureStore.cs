using StackExchange.Redis;

namespace HubPay.Infrastructure.Redis;

public sealed class RedisFeatureStore
{
    private readonly IConnectionMultiplexer _redis;

    public RedisFeatureStore(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<int> GetDeviceTransactionCountAsync(string deviceFingerprint, TimeSpan window, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"hubpay:features:device:{deviceFingerprint}";
        var count = await db.StringGetAsync(key);
        if (!count.HasValue)
        {
            await db.StringSetAsync(key, "1", window);
            return 1;
        }
        var newCount = await db.StringIncrementAsync(key);
        return (int)newCount;
    }

    public async Task<int> GetEmailCountryCountAsync(string email, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"hubpay:features:email:countries:{email.ToLowerInvariant()}";
        var count = await db.SetLengthAsync(key);
        return (int)count;
    }

    public async Task RecordEmailCountryAsync(string email, string countryCode, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"hubpay:features:email:countries:{email.ToLowerInvariant()}";
        await db.SetAddAsync(key, countryCode);
        await db.KeyExpireAsync(key, TimeSpan.FromHours(1));
    }

    public async Task IncrementDeviceCounterAsync(string deviceFingerprint, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"hubpay:features:device:{deviceFingerprint}";
        if (!await db.KeyExistsAsync(key))
            await db.StringSetAsync(key, "1", TimeSpan.FromMinutes(5));
        else
            await db.StringIncrementAsync(key);
    }
}
