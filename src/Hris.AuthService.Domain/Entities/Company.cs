namespace Hris.AuthService.Domain.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<User> Users { get; set; } = new();
    public List<Role> Roles { get; set; } = new();
}
