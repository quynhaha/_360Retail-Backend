using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace _360Retail.Services.Sales.Infrastructure.Services;

/// <summary>
/// Redis cache wrapper for Sales service
/// </summary>
public class CacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Get from cache, or execute factory and cache the result
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
    {
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<T>(cached)!;

        var data = await factory();
        var json = JsonSerializer.Serialize(data);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(key, json, options);
        return data;
    }

    /// <summary>
    /// Remove a specific key from cache
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    /// <summary>
    /// Remove all keys matching a pattern (prefix-based)
    /// </summary>
    public async Task RemoveByPrefixAsync(string prefix)
    {
        // StackExchange.Redis IDistributedCache doesn't support KEYS/SCAN directly
        // For simplicity, we track individual keys. For production, use IConnectionMultiplexer.
        // This is a no-op placeholder — individual invalidation via RemoveAsync is used instead.
        await Task.CompletedTask;
    }
}
