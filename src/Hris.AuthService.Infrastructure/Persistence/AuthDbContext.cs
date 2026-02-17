using Hris.AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hris.AuthService.Infrastructure.Persistence;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<UserProfilePhoto> UserProfilePhotos => Set<UserProfilePhoto>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("pgcrypto");

        b.Entity<Company>(e =>
        {
            e.ToTable("companies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Username).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Username }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Email }).IsUnique();

            e.HasOne(x => x.Profile).WithOne(x => x.User).HasForeignKey<UserProfile>(x => x.UserId);
            e.HasOne(x => x.Preference).WithOne(x => x.User).HasForeignKey<UserPreference>(x => x.UserId);
            e.HasOne(x => x.Photo).WithOne(x => x.User).HasForeignKey<UserProfilePhoto>(x => x.UserId);
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        b.Entity<UserProfile>(e =>
        {
            e.ToTable("user_profiles");
            e.HasKey(x => x.UserId);
        });

        b.Entity<UserProfilePhoto>(e =>
        {
            e.ToTable("user_profile_photos");
            e.HasKey(x => x.UserId);
            e.Property(x => x.PhotoUrl).IsRequired();
        });

        b.Entity<UserPreference>(e =>
        {
            e.ToTable("user_preferences");
            e.HasKey(x => x.UserId);
            // store as jsonb, but keep property as string for simplicity
            e.Property(x => x.PrefsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.TokenHash).IsRequired();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.TokenHash);
        });

        base.OnModelCreating(b);
    }
}
