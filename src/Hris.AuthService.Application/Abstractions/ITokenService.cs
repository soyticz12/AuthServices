using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(User user, IReadOnlyCollection<string> roles);
    string GenerateRefreshTokenPlain();
    string HashRefreshToken(string refreshTokenPlain);
}
