using Hris.AuthService.Application.Abstractions;

namespace Hris.AuthService.Infrastructure.Security;

public sealed class JwtOptions : IJwtOptions
{
    public string Issuer { get; init; } = "Hris.AuthService";
    public string Audience { get; init; } = "Hris.Client";
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 14;
}
