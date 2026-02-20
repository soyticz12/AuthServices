using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Hris.AuthService.Infrastructure.Security;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _opt;

    public TokenService(JwtOptions opt) => _opt = opt;

    public string CreateAccessToken(User user, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(_opt.Key) || _opt.Key.Length < 32)
            throw new InvalidOperationException("JWT Key must be at least 32 characters.");

        var now = DateTimeOffset.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

            new("company_id", user.CompanyId.ToString()),
            new(ClaimTypes.Name, user.Username),
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.UtcDateTime.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshTokenPlain()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public string HashRefreshToken(string refreshTokenPlain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenPlain));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] data)
        => Base64UrlEncoder.Encode(data);
}
