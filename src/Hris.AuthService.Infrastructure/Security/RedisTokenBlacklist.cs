// Hris.AuthService.Infrastructure/Security/RedisTokenBlacklist.cs
using Hris.AuthService.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Hris.AuthService.Infrastructure.Security;

public sealed class RedisTokenBlacklist : ITokenBlacklist
{
    private readonly IDistributedCache _cache;

    public RedisTokenBlacklist(IDistributedCache cache)
        => _cache = cache;

    public async Task AddAsync(string jti, TimeSpan expiresIn, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiresIn
        };

        await _cache.SetAsync(
            $"blacklist:{jti}",
            Encoding.UTF8.GetBytes("1"),
            options,
            ct);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct)
    {
        var value = await _cache.GetAsync($"blacklist:{jti}", ct);
        return value is not null;
    }
}