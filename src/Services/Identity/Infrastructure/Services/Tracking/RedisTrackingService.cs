using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace _360Retail.Services.Identity.Infrastructure.Services.Tracking;

public class RedisTrackingService
{
    private readonly IDistributedCache _cache;

    public RedisTrackingService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task IncrementPageViewAsync(string date)
    {
        var key = $"tracking:landing:views:{date}";
        var value = await _cache.GetStringAsync(key);
        
        long count = string.IsNullOrEmpty(value) ? 1 : long.Parse(value) + 1;
        await _cache.SetStringAsync(key, count.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) // Keep for 30 days
        });
    }

    public async Task<long> GetPageViewsAsync(string date)
    {
        var key = $"tracking:landing:views:{date}";
        var value = await _cache.GetStringAsync(key);
        return string.IsNullOrEmpty(value) ? 0 : long.Parse(value);
    }
}
