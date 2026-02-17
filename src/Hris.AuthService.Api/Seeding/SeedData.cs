using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Hris.AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Api.Seeding;

public static class SeedData
{
    public static async Task EnsureSeededAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasherAdapter>();

        await db.Database.MigrateAsync();

        // Company
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Code == "DEFAULT");
        if (company == null)
        {
            company = new Company { Name = "Default Company", Code = "DEFAULT" };
            db.Companies.Add(company);
            await db.SaveChangesAsync();
        }

        // Roles
        var neededRoles = new[] { "Admin", "HR", "Finance", "Employee" };
        foreach (var r in neededRoles)
        {
            var exists = await db.Roles.AnyAsync(x => x.CompanyId == company.Id && x.Name == r);
            if (!exists) db.Roles.Add(new Role { CompanyId = company.Id, Name = r });
        }
        await db.SaveChangesAsync();

        // Admin user: admin / admin123 (change later)
        var admin = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.CompanyId == company.Id && u.Username == "admin");

        if (admin == null)
        {
            admin = new User
            {
                CompanyId = company.Id,
                Username = "admin",
                Email = "admin@local",
                IsActive = true
            };
            admin.PasswordHash = hasher.Hash(admin, "admin123");
            db.Users.Add(admin);
            await db.SaveChangesAsync(); // Save user first to get the ID

            // Now add related entities with the actual UserId
            db.UserProfiles.Add(new UserProfile { UserId = admin.Id, FirstName = "System", LastName = "Admin" });
            db.UserPreferences.Add(new UserPreference { UserId = admin.Id, PrefsJson = """{"theme":"system"}""" });

            await db.SaveChangesAsync();
        }

        // Attach Admin role
        var adminRole = await db.Roles.FirstAsync(r => r.CompanyId == company.Id && r.Name == "Admin");
        var hasAdminRole = await db.UserRoles.AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == adminRole.Id);
        if (!hasAdminRole)
        {
            db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }
    }
}