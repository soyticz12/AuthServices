namespace Hris.AuthService.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Username { get; set; } = "";
    public string? Email { get; set; }

    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public UserProfile? Profile { get; set; }
    public UserPreference? Preference { get; set; }
    public UserProfilePhoto? Photo { get; set; }

    public List<UserRole> UserRoles { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}

public class UserProfile
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Address { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class UserPreference
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // stored in jsonb
    public string PrefsJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class UserProfilePhoto
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string PhotoUrl { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}