using Microsoft.Extensions.Caching.Distributed;

namespace _360Retail.Services.Identity.Infrastructure.Services;

/// <summary>
/// Redis-backed service for blacklisting JWT tokens on logout.
/// When a user logs out, their token is added to Redis with TTL matching the token's remaining lifetime.
/// </summary>
public class TokenBlacklistService
{
    private readonly IDistributedCache _cache;
    private const string Prefix = "blacklist:";

    public TokenBlacklistService(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Blacklist a JWT token (called on logout) 
    /// </summary>
    public async Task BlacklistAsync(string token, TimeSpan remainingLifetime)
    {
        var key = Prefix + token;
        await _cache.SetStringAsync(key, "revoked", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = remainingLifetime
        });
    }

    /// <summary>
    /// Check if a token is blacklisted
    /// </summary>
    public async Task<bool> IsBlacklistedAsync(string token)
    {
        var result = await _cache.GetStringAsync(Prefix + token);
        return result != null;
    }
}
