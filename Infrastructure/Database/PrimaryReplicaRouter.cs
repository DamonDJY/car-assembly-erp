using CarAssemblyErp.Infrastructure.Redis;

namespace CarAssemblyErp.Infrastructure.Database;

public class PrimaryReplicaRouter : IConnectionRouter
{
    private readonly ICacheService _cache;
    private readonly string _primary;
    private readonly string _replica1;
    private readonly string _replica2;
    private int _replicaIndex = 0;

    public PrimaryReplicaRouter(IConfiguration config, ICacheService cache)
    {
        _cache = cache;
        _primary = config.GetConnectionString("Primary")!;
        _replica1 = config.GetConnectionString("Replica1")!;
        _replica2 = config.GetConnectionString("Replica2")!;
    }

    public string GetPrimaryConnectionString() => _primary;

    public string GetReplicaConnectionString()
    {
        // Round-robin 轮询
        var index = Interlocked.Increment(ref _replicaIndex);
        return (index % 2 == 0) ? _replica1 : _replica2;
    }

    public async Task<bool> IsRecentlyWrittenAsync(string entityKey)
    {
        return await _cache.ExistsAsync($"write:{entityKey}");
    }

    public async Task MarkAsWrittenAsync(string entityKey, TimeSpan ttl)
    {
        await _cache.SetAsync($"write:{entityKey}", "1", absoluteExpiration: ttl);
    }
}
