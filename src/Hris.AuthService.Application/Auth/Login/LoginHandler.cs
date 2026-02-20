using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Application.Common;
using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Auth.Login;

public sealed class LoginHandler
{
    private readonly ICompanyRepository _companies;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IJwtOptions _jwt;

    public LoginHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher hasher,
        ITokenService tokens,
        IJwtOptions jwt)
    {
        _companies = companies;
        _users = users;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _tokens = tokens;
        _jwt = jwt;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand cmd, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var company = await _companies.FindByCodeAsync(cmd.CompanyCode, ct);
        if (company is null)
            return Result<LoginResponse>.Fail("Invalid company.", 401);

        var user = await _users.FindByCompanyAndUsernameAsync(company.Id, cmd.Username, ct);
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Fail("Invalid credentials.", 401);

        if (!_hasher.Verify(user, cmd.Password))
            return Result<LoginResponse>.Fail("Invalid credentials.", 401);

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        user.LastLoginAt = DateTimeOffset.UtcNow;

        // refresh token create + store hash
        var refreshPlain = _tokens.GenerateRefreshTokenPlain();
        var refreshHash = _tokens.HashRefreshToken(refreshPlain);

        var rt = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = ip,
            UserAgent = userAgent
        };

        await _refreshTokens.AddAsync(rt, ct);

        // user.LastLoginAt changed; user is tracked by EF in repository implementation
        await _refreshTokens.SaveChangesAsync(ct);

        var access = _tokens.CreateAccessToken(user, roles);
        return Result<LoginResponse>.Ok(new LoginResponse(access, refreshPlain));
    }
}
