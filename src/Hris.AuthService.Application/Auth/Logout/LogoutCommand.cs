namespace Hris.AuthService.Application.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken, string? AccessTokenJti, DateTime? AccessTokenExpiry);