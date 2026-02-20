using Hris.AuthService.Api.Security;
using Hris.AuthService.Application.Auth.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hris.AuthService.Api.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly CreateUserHandler _create;

    public AdminUsersController(CreateUserHandler create) => _create = create;

    public record CreateUserRequest(
        string Username,
        string Password,
        string? Email,
        string FirstName,
        string LastName,
        string[] Roles
    );

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var companyId = CurrentUser.CompanyId(User);

        var result = await _create.Handle(
            new CreateUserCommand(companyId, req.Username, req.Password, req.Email, req.FirstName, req.LastName, req.Roles),
            ct);

        if (!result.IsSuccess) return StatusCode(result.StatusCode, result.Error);
        return Ok(new { id = result.Value });
    }
}
