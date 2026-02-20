namespace Hris.AuthService.Application.Abstractions;

public interface IJwtOptions
{
    int RefreshTokenDays { get; }
    int AccessTokenMinutes { get; }
    string Issuer { get; }
    string Audience { get; }
    string Key { get; }
}
