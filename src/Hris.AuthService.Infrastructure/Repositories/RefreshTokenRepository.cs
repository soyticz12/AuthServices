using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Domain.Entities;
using Hris.AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _db;
    public RefreshTokenRepository(AuthDbContext db) => _db = db;

    public Task<RefreshToken?> FindByHashWithUserAsync(string tokenHash, CancellationToken ct) =>
        _db.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(u => u!.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        _db.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
