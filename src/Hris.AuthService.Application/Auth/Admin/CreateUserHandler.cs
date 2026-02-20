using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Application.Common;
using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Auth.Admin;

public sealed class CreateUserHandler
{
    private readonly IAdminUserRepository _repo;
    private readonly IPasswordHasher _hasher;

    public CreateUserHandler(IAdminUserRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Username))
            return Result<Guid>.Fail("Username is required.", 400);

        if (string.IsNullOrWhiteSpace(cmd.Password))
            return Result<Guid>.Fail("Password is required.", 400);

        var exists = await _repo.UsernameExistsAsync(cmd.CompanyId, cmd.Username, ct);
        if (exists)
            return Result<Guid>.Fail("Username already exists.", 400);

        var requestedRoleNames = (cmd.Roles ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // roles must exist in this company
        var roles = await _repo.GetRolesByNamesAsync(cmd.CompanyId, requestedRoleNames, ct);

        // validate missing roles
        var found = new HashSet<string>(roles.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);
        var missing = requestedRoleNames.Where(r => !found.Contains(r)).ToArray();
        if (missing.Length > 0)
            return Result<Guid>.Fail($"Unknown role(s): {string.Join(", ", missing)}", 400);

        var user = new User
        {
            CompanyId = cmd.CompanyId,
            Username = cmd.Username.Trim(),
            Email = string.IsNullOrWhiteSpace(cmd.Email) ? null : cmd.Email.Trim(),
            IsActive = true
        };
        user.PasswordHash = _hasher.Hash(user, cmd.Password);

        var profile = new UserProfile
        {
            // UserId is set in repository after user is saved
            FirstName = cmd.FirstName?.Trim() ?? "",
            LastName = cmd.LastName?.Trim() ?? ""
        };

        var preference = new UserPreference
        {
            PrefsJson = """{"theme":"system"}"""
        };

        var newUserId = await _repo.CreateUserWithDefaultsAsync(user, profile, preference, roles, ct);
        return Result<Guid>.Ok(newUserId);
    }
}
