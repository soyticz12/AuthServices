namespace Hris.AuthService.Application.Abstractions;

public interface ITokenBlacklist
{
    Task AddAsync(string jti, TimeSpan expiresIn, CancellationToken ct);
    Task<bool> IsRevokedAsync(string jti, CancellationToken ct);
}