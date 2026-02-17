namespace Hris.AuthService.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = "";

    public List<UserRole> UserRoles { get; set; } = new();
}

public class UserRole
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}