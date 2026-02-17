using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Hris.AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly TokenService _tokens;
    private readonly PasswordHasherAdapter _hasher;
    private readonly JwtOptions _jwt;

    public AuthController(AuthDbContext db, TokenService tokens, PasswordHasherAdapter hasher, JwtOptions jwt)
    {
        _db = db;
        _tokens = tokens;
        _hasher = hasher;
        _jwt = jwt;
    }

    public record LoginRequest(string Username, string Password, string CompanyCode);
    public record LoginResponse(string AccessToken, string RefreshToken);

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Code == req.CompanyCode);
        if (company == null) return Unauthorized("Invalid company.");

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.CompanyId == company.Id && u.Username == req.Username);

        if (user == null || !user.IsActive) return Unauthorized("Invalid credentials.");
        if (!_hasher.Verify(user, req.Password)) return Unauthorized("Invalid credentials.");

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        user.LastLoginAt = DateTimeOffset.UtcNow;

        // Create refresh token (store hash)
        var refreshPlain = TokenService.GenerateRefreshToken();
        var refreshHash = TokenService.HashToken(refreshPlain);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });

        await _db.SaveChangesAsync();

        var access = _tokens.CreateAccessToken(user, roles);
        return Ok(new LoginResponse(access, refreshPlain));
    }

    public record RefreshRequest(string RefreshToken);
    public record RefreshResponse(string AccessToken, string RefreshToken);

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshResponse>> Refresh([FromBody] RefreshRequest req)
    {
        var hash = TokenService.HashToken(req.RefreshToken);

        var rt = await _db.RefreshTokens
            .Include(x => x.User).ThenInclude(u => u!.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == hash);

        if (rt == null || !rt.IsActive || rt.User == null || !rt.User.IsActive)
            return Unauthorized("Invalid refresh token.");

        // rotate refresh token
        rt.RevokedAt = DateTimeOffset.UtcNow;
        var newPlain = TokenService.GenerateRefreshToken();
        var newHash = TokenService.HashToken(newPlain);

        var newRt = new RefreshToken
        {
            UserId = rt.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
        };
        rt.ReplacedByTokenId = newRt.Id;

        _db.RefreshTokens.Add(newRt);
        await _db.SaveChangesAsync();

        var roles = rt.User.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var access = _tokens.CreateAccessToken(rt.User, roles);

        return Ok(new RefreshResponse(access, newPlain));
    }

    public record LogoutRequest(string RefreshToken);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {
        var hash = TokenService.HashToken(req.RefreshToken);
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (rt == null) return Ok(); // idempotent
        rt.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
