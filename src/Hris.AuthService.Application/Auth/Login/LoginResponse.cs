namespace Hris.AuthService.Application.Auth.Login;

public sealed record LoginResponse(string AccessToken, string RefreshToken);
