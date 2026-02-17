using Hris.AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Hris.AuthService.Infrastructure.Security;

public class PasswordHasherAdapter
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User u, string password) => _hasher.HashPassword(u, password);

    public bool Verify(User u, string password)
    {
        var r = _hasher.VerifyHashedPassword(u, u.PasswordHash, password);
        return r == PasswordVerificationResult.Success || r == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
