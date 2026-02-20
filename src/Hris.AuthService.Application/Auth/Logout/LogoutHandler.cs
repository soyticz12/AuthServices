using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Application.Common;

namespace Hris.AuthService.Application.Auth.Logout;

public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly ITokenBlacklist _blacklist;

    public LogoutHandler(IRefreshTokenRepository tokens, ITokenBlacklist blacklist)
    {
        _tokens = tokens;
        _blacklist = blacklist;
    }

    public async Task<Result<bool>> Handle(LogoutCommand cmd, string? ip, string? ua, CancellationToken ct)
    {
        await _tokens.RevokeByRawTokenAsync(cmd.RefreshToken, ip, ua, ct);

        if (!string.IsNullOrEmpty(cmd.AccessTokenJti) && cmd.AccessTokenExpiry.HasValue)
        {
            var remaining = cmd.AccessTokenExpiry.Value - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
                await _blacklist.AddAsync(cmd.AccessTokenJti, remaining, ct);
        }

        return Result<bool>.Ok(true);
    }
}