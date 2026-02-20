namespace Hris.AuthService.Application.Auth.Admin;

public sealed record CreateUserCommand(
    Guid CompanyId,
    string Username,
    string Password,
    string? Email,
    string FirstName,
    string LastName,
    string[] Roles
);
