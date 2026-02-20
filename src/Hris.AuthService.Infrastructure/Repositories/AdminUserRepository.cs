using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Infrastructure.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly AuthDbContext _db;

    public AdminUserRepository(AuthDbContext db) => _db = db;

    public Task<bool> UsernameExistsAsync(Guid companyId, string username, CancellationToken ct)
    {
        var u = username.Trim();

        return _db.Users.AnyAsync(x => x.CompanyId == companyId && x.Username == u, ct);
    }

    public Task<List<Role>> GetRolesByNamesAsync(Guid companyId, IEnumerable<string> roleNames, CancellationToken ct)
    {
        var names = roleNames.ToArray();
        if (names.Length == 0) return Task.FromResult(new List<Role>());

        return _db.Roles
            .Where(r => r.CompanyId == companyId && names.Contains(r.Name))
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateUserWithDefaultsAsync(
        User user,
        UserProfile profile,
        UserPreference preference,
        IEnumerable<Role> roles,
        CancellationToken ct)
    {
        // 1) Save user 
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // 2) attach profile + prefs 
        profile.UserId = user.Id;
        preference.UserId = user.Id;

        _db.UserProfiles.Add(profile);
        _db.UserPreferences.Add(preference);

        // 3) UserRoles
        foreach (var r in roles)
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = r.Id });

        await _db.SaveChangesAsync(ct);
        return user.Id;
    }
}
