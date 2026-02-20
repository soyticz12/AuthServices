namespace Hris.AuthService.Application.Abstractions;

public interface ILoginThrottler
{
    Task<(bool IsLocked, int RetryAfterSeconds)> IsLockedAsync(string companyCode, string username, string? ip, CancellationToken ct);
    Task<(bool LockedNow, int RetryAfterSeconds, int AttemptCount)> RegisterFailureAsync(string companyCode, string username, string? ip, CancellationToken ct);
    Task ClearAsync(string companyCode, string username, string? ip, CancellationToken ct);
}
