namespace Hris.AuthService.Application.Auth.Login;

public sealed record LoginCommand(string Username, string Password, string CompanyCode);
