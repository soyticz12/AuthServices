using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Abstractions;

public interface IAdminUserRepository
{
    Task<bool> UsernameExistsAsync(Guid companyId, string username, CancellationToken ct);
    Task<List<Role>> GetRolesByNamesAsync(Guid companyId, IEnumerable<string> roleNames, CancellationToken ct);
    Task<Guid> CreateUserWithDefaultsAsync(
        User user,
        UserProfile profile,
        UserPreference preference,
        IEnumerable<Role> roles,
        CancellationToken ct);
}
