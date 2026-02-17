using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hris.AuthService.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Hris.AuthService.Infrastructure.Security;

public class TokenService
{
    private readonly JwtOptions _opt;

    public TokenService(JwtOptions opt) => _opt = opt;

    public string CreateAccessToken(User user, IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(_opt.Key) || _opt.Key.Length < 32)
            throw new InvalidOperationException("JWT Key must be at least 32 characters.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("company_id", user.CompanyId.ToString()),
            new(ClaimTypes.Name, user.Username),
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r)); // RBAC via claims

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateRefreshToken()
    {
        // 32 bytes -> 43 char base64url-ish
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes); // deterministic hex hash
    }
}
