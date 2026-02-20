using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public Task<User?> FindByCompanyAndUsernameAsync(Guid companyId, string username, CancellationToken ct) =>
        _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.CompanyId == companyId && u.Username == username, ct);

    public Task<User?> GetByIdAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.CompanyId == companyId && u.Id == userId, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
