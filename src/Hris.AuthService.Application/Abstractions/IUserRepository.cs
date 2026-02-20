using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByCompanyAndUsernameAsync(Guid companyId, string username, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid companyId, Guid userId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
