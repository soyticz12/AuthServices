using Hris.AuthService.Application.Abstractions;
using StackExchange.Redis;

namespace Hris.AuthService.Api.Security;

public sealed class RedisLoginThrottler : ILoginThrottler
{
    private readonly IDatabase _db;
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockStep;
    private readonly TimeSpan _lockLevelTtl;

    public RedisLoginThrottler(IConnectionMultiplexer mux, IConfiguration config)
    {
        _db = mux.GetDatabase();

        _maxAttempts = int.TryParse(config["LOGIN_MAX_ATTEMPTS"], out var a) ? a : 3;

        var stepSeconds = int.TryParse(config["LOGIN_LOCK_STEP_SECONDS"], out var s) ? s : 900;
        _lockStep = TimeSpan.FromSeconds(stepSeconds);

        var levelTtlSeconds = int.TryParse(config["LOGIN_LOCK_LEVEL_TTL_SECONDS"], out var t) ? t : 86400;
        _lockLevelTtl = TimeSpan.FromSeconds(levelTtlSeconds);
    }

    private static string Norm(string s) => (s ?? "").Trim().ToLowerInvariant();

    private static string FailKey(string company, string user) => $"auth:login:fail:{company}:{user}";
    private static string LockKey(string company, string user) => $"auth:login:lock:{company}:{user}";
    private static string LevelKey(string company, string user) => $"auth:login:level:{company}:{user}";

    public async Task<(bool IsLocked, int RetryAfterSeconds)> IsLockedAsync(
        string companyCode, string username, string? ip, CancellationToken ct)
    {
        companyCode = Norm(companyCode);
        username = Norm(username);

        var lockKey = LockKey(companyCode, username);

        if (!await _db.KeyExistsAsync(lockKey))
            return (false, 0);

        var ttl = await _db.KeyTimeToLiveAsync(lockKey);
        var seconds = ttl.HasValue ? Math.Max(1, (int)ttl.Value.TotalSeconds) : (int)_lockStep.TotalSeconds;
        return (true, seconds);
    }

    public async Task<(bool LockedNow, int RetryAfterSeconds, int AttemptCount)> RegisterFailureAsync(
        string companyCode, string username, string? ip, CancellationToken ct)
    {
        companyCode = Norm(companyCode);
        username = Norm(username);

        var lockKey = LockKey(companyCode, username);
        var failKey = FailKey(companyCode, username);
        var levelKey = LevelKey(companyCode, username);

        // If already locked, just return remaining TTL
        if (await _db.KeyExistsAsync(lockKey))
        {
            var ttl = await _db.KeyTimeToLiveAsync(lockKey);
            var seconds = ttl.HasValue ? Math.Max(1, (int)ttl.Value.TotalSeconds) : (int)_lockStep.TotalSeconds;
            return (true, seconds, _maxAttempts);
        }

        // Count failures (consecutive until success; we reset on successful login)
        var count = (int)await _db.StringIncrementAsync(failKey);

        // Keep failures around for a while so they don't last forever
        if (count == 1)
            await _db.KeyExpireAsync(failKey, _lockLevelTtl);

        // If hit threshold => lock
        if (count >= _maxAttempts)
        {
            // Increase lock level (1 => 15m, 2 => 30m, 3 => 45m, ...)
            var level = (int)await _db.StringIncrementAsync(levelKey);
            await _db.KeyExpireAsync(levelKey, _lockLevelTtl);

            var lockDuration = TimeSpan.FromSeconds(_lockStep.TotalSeconds * level);

            await _db.StringSetAsync(lockKey, "1", lockDuration);

            // Reset attempt counter for the next cycle after lock expires
            await _db.KeyDeleteAsync(failKey);

            return (true, (int)lockDuration.TotalSeconds, count);
        }

        // Not locked yet
        return (false, 0, count);
    }

    public async Task ClearAsync(string companyCode, string username, string? ip, CancellationToken ct)
    {
        companyCode = Norm(companyCode);
        username = Norm(username);

        await _db.KeyDeleteAsync(FailKey(companyCode, username));
        await _db.KeyDeleteAsync(LockKey(companyCode, username));
        await _db.KeyDeleteAsync(LevelKey(companyCode, username)); // ✅ reset escalation on success
    }
}
