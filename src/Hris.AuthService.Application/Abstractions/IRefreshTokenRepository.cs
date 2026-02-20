using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashWithUserAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task<bool> RevokeByRawTokenAsync(string rawToken, string? revokedByIp, string? userAgent, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

}
