using Hris.AuthService.Domain.Entities;

namespace Hris.AuthService.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}
