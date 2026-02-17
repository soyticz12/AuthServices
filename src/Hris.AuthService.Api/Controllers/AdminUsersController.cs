using Hris.AuthService.Api.Security;
using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Hris.AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Api.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly PasswordHasherAdapter _hasher;

    public AdminUsersController(AuthDbContext db, PasswordHasherAdapter hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public record CreateUserRequest(
        string Username,
        string Password,
        string? Email,
        string FirstName,
        string LastName,
        string[] Roles
    );

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        var companyId = CurrentUser.CompanyId(User);

        var exists = await _db.Users.AnyAsync(u => u.CompanyId == companyId && u.Username == req.Username);
        if (exists) return BadRequest("Username already exists.");

        var user = new User
        {
            CompanyId = companyId,
            Username = req.Username,
            Email = req.Email,
            IsActive = true
        };
        user.PasswordHash = _hasher.Hash(user, req.Password);

        _db.Users.Add(user);

        _db.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            FirstName = req.FirstName,
            LastName = req.LastName
        });

        _db.UserPreferences.Add(new UserPreference
        {
            UserId = user.Id,
            PrefsJson = """{"theme":"system"}"""
        });

        // attach roles (must exist for this company)
        var roles = await _db.Roles.Where(r => r.CompanyId == companyId && req.Roles.Contains(r.Name)).ToListAsync();
        foreach (var r in roles)
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = r.Id });

        await _db.SaveChangesAsync();
        return Ok(new { user.Id });
    }
}
