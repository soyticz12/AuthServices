namespace Hris.AuthService.Application.Auth.Refresh;

public sealed record RefreshResponse(string AccessToken, string RefreshToken);
