using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Application.Common;
using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Auth.Refresh;

public sealed class RefreshHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokens;
    private readonly IJwtOptions _jwt;

    public RefreshHandler(IRefreshTokenRepository refreshTokens, ITokenService tokens, IJwtOptions jwt)
    {
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _jwt = jwt;
    }

    public async Task<Result<RefreshResponse>> Handle(RefreshCommand cmd, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(cmd.RefreshToken);

        var rt = await _refreshTokens.FindByHashWithUserAsync(hash, ct);

        if (rt is null || !rt.IsActive || rt.User is null || !rt.User.IsActive)
            return Result<RefreshResponse>.Fail("Invalid refresh token.", 401);

        if (rt.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result<RefreshResponse>.Fail("Refresh token expired.", 401);

        // rotate refresh token
        rt.RevokedAt = DateTimeOffset.UtcNow;

        var newPlain = _tokens.GenerateRefreshTokenPlain();
        var newHash = _tokens.HashRefreshToken(newPlain);

        var newRt = new RefreshToken
        {
            UserId = rt.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = ip,
            UserAgent = userAgent
        };

        await _refreshTokens.AddAsync(newRt, ct);

        // Save once to generate newRt.Id
        await _refreshTokens.SaveChangesAsync(ct);

        // Now you can safely link
        rt.ReplacedByTokenId = newRt.Id;

        await _refreshTokens.SaveChangesAsync(ct);

        var roles = rt.User.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var access = _tokens.CreateAccessToken(rt.User, roles);

        return Result<RefreshResponse>.Ok(new RefreshResponse(access, newPlain));
    }
}
