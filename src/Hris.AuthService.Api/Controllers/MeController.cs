using Hris.AuthService.Api.Security;
using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly AuthDbContext _db;

    public MeController(AuthDbContext db) => _db = db;

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = CurrentUser.UserId(User);
        var companyId = CurrentUser.CompanyId(User);

        var u = await _db.Users
            .Include(x => x.Profile)
            .Include(x => x.Photo)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId);

        if (u == null) return NotFound();

        return Ok(new
        {
            u.Id,
            u.Username,
            u.Email,
            Profile = u.Profile == null ? null : new
            {
                u.Profile.FirstName,
                u.Profile.LastName,
                u.Profile.Phone,
                u.Profile.Department,
                u.Profile.JobTitle,
                u.Profile.UpdatedAt
            },
            PhotoUrl = u.Photo?.PhotoUrl
        });
    }


    public record UpdateProfileRequest(string? FirstName, string? LastName, string? Phone, string? Department, string? JobTitle);

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = CurrentUser.UserId(User);
        var companyId = CurrentUser.CompanyId(User);

        var u = await _db.Users.Include(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId);

        if (u == null) return NotFound();

        u.Profile ??= new UserProfile { UserId = u.Id };
        u.Profile.FirstName = req.FirstName;
        u.Profile.LastName = req.LastName;
        u.Profile.Phone = req.Phone;
        u.Profile.Department = req.Department;
        u.Profile.JobTitle = req.JobTitle;
        u.Profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = CurrentUser.UserId(User);
        var companyId = CurrentUser.CompanyId(User);

        var u = await _db.Users.Include(x => x.Preference)
            .FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId);

        if (u == null) return NotFound();

        return Ok(new { prefs = u.Preference?.PrefsJson ?? "{}" });
    }

    public record UpdatePreferencesRequest(string PrefsJson);

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest req)
    {
        var userId = CurrentUser.UserId(User);
        var companyId = CurrentUser.CompanyId(User);

        var u = await _db.Users.Include(x => x.Preference)
            .FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId);

        if (u == null) return NotFound();

        u.Preference ??= new UserPreference { UserId = u.Id };
        u.Preference.PrefsJson = string.IsNullOrWhiteSpace(req.PrefsJson) ? "{}" : req.PrefsJson;
        u.Preference.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    public record UpdatePhotoRequest(string PhotoUrl);

    [HttpPut("photo")]
    public async Task<IActionResult> UpdatePhoto([FromBody] UpdatePhotoRequest req)
    {
        var userId = CurrentUser.UserId(User);
        var companyId = CurrentUser.CompanyId(User);

        var u = await _db.Users.Include(x => x.Photo)
            .FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId);

        if (u == null) return NotFound();

        u.Photo ??= new UserProfilePhoto { UserId = u.Id };
        u.Photo.PhotoUrl = req.PhotoUrl;
        u.Photo.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }
}
