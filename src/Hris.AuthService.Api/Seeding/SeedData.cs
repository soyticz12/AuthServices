using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Api.Seeding;

public static class SeedData
{
    public static async Task EnsureSeededAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync();

        // 1) Companies
        var finteq = await EnsureCompanyAsync(db, "Finteq", "FINTEQ");
        var anb = await EnsureCompanyAsync(db, "AnB", "ANB");

        // 2) System roles per company
        var systemRoles = new[] { "Admin", "Finance", "Standard User" };

        await EnsureRolesAsync(db, finteq.Id, systemRoles);
        await EnsureRolesAsync(db, anb.Id, systemRoles);

        // 3) Sample users (per company)
        // Passwords: change later
        await EnsureUserWithRoleAsync(db, hasher,
            companyId: finteq.Id,
            username: "finteq_admin",
            email: "admin@finteq.local",
            password: "admin123",
            firstName: "Finteq",
            lastName: "Admin",
            systemRoleName: "Admin",
            workRole: "President"
        );

        await EnsureUserWithRoleAsync(db, hasher,
            companyId: finteq.Id,
            username: "finteq_finance",
            email: "finance@finteq.local",
            password: "finance123",
            firstName: "Finteq",
            lastName: "Finance",
            systemRoleName: "Finance",
            workRole: "Manager"
        );

        await EnsureUserWithRoleAsync(db, hasher,
            companyId: finteq.Id,
            username: "finteq_user",
            email: "user@finteq.local",
            password: "user123",
            firstName: "Finteq",
            lastName: "User",
            systemRoleName: "Standard User",
            workRole: "Standard Employee"
        );

        await EnsureUserWithRoleAsync(db, hasher,
            companyId: anb.Id,
            username: "anb_admin",
            email: "admin@anb.local",
            password: "admin123",
            firstName: "AnB",
            lastName: "Admin",
            systemRoleName: "Admin",
            workRole: "Vice President"
        );

        await EnsureUserWithRoleAsync(db, hasher,
            companyId: anb.Id,
            username: "anb_finance",
            email: "finance@anb.local",
            password: "finance123",
            firstName: "AnB",
            lastName: "Finance",
            systemRoleName: "Finance",
            workRole: "TL"
        );

        await EnsureUserWithRoleAsync(db, hasher,
            companyId: anb.Id,
            username: "anb_user",
            email: "user@anb.local",
            password: "user123",
            firstName: "AnB",
            lastName: "User",
            systemRoleName: "Standard User",
            workRole: "Standard Employee"
        );
    }

    private static async Task<Company> EnsureCompanyAsync(AuthDbContext db, string name, string code)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Code == code);
        if (company != null) return company;

        company = new Company { Name = name, Code = code };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    private static async Task EnsureRolesAsync(AuthDbContext db, Guid companyId, IEnumerable<string> roleNames)
    {
        foreach (var roleName in roleNames)
        {
            var exists = await db.Roles.AnyAsync(r => r.CompanyId == companyId && r.Name == roleName);
            if (!exists)
            {
                db.Roles.Add(new Role { CompanyId = companyId, Name = roleName });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserWithRoleAsync(
        AuthDbContext db,
        IPasswordHasher hasher,
        Guid companyId,
        string username,
        string email,
        string password,
        string firstName,
        string lastName,
        string systemRoleName,
        string workRole
    )
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.CompanyId == companyId && u.Username == username);

        if (user == null)
        {
            user = new User
            {
                CompanyId = companyId,
                Username = username,
                Email = email,
                IsActive = true,
                PasswordHash = "" // set below
            };
            user.PasswordHash = hasher.Hash(user, password);

            db.Users.Add(user);
            await db.SaveChangesAsync(); // to get user.Id

            // Profile
            db.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName
            });

            // Preferences (store work role here for now)
            db.UserPreferences.Add(new UserPreference
            {
                UserId = user.Id,
                PrefsJson = $$"""{"theme":"system","workRole":"{{workRole}}"}"""
            });

            await db.SaveChangesAsync();
        }
        else
        {
            // Ensure preferences exist (optional)
            var pref = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (pref == null)
            {
                db.UserPreferences.Add(new UserPreference
                {
                    UserId = user.Id,
                    PrefsJson = $$"""{"theme":"system","workRole":"{{workRole}}"}"""
                });
                await db.SaveChangesAsync();
            }
        }

        // Attach system role
        var role = await db.Roles.FirstAsync(r => r.CompanyId == companyId && r.Name == systemRoleName);
        var hasRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        if (!hasRole)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
        }
    }
}
