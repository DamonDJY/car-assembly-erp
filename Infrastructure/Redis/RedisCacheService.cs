using System.Text.Json;
using StackExchange.Redis;

namespace CarAssemblyErp.Infrastructure.Redis;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue) return default;
        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        var expiry = absoluteExpiration ?? TimeSpan.FromHours(1);
        
        // 缓存雪崩防护：添加随机偏移 (0-60秒)
        var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 60));
        expiry = expiry.Add(jitter);

        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task SetEmptyAsync(string key, TimeSpan expiration, CancellationToken ct = default)
    {
        await _db.StringSetAsync($"{key}:empty", "1", expiration);
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiration, CancellationToken ct = default)
    {
        return await _db.StringSetAsync($"lock:{key}", "1", expiration, When.NotExists);
    }

    public async Task ReleaseLockAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync($"lock:{key}");
    }
}
