namespace CarAssemblyErp.Infrastructure.Redis;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task SetEmptyAsync(string key, TimeSpan expiration, CancellationToken ct = default);
    Task<bool> AcquireLockAsync(string key, TimeSpan expiration, CancellationToken ct = default);
    Task ReleaseLockAsync(string key, CancellationToken ct = default);
}
