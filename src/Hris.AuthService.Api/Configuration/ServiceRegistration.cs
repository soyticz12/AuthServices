using System.Security.Claims;
using System.Text;
using Hris.AuthService.Api.Security;
using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Application.Auth.Admin;
using Hris.AuthService.Application.Auth.Login;
using Hris.AuthService.Application.Auth.Refresh;
using Hris.AuthService.Infrastructure.Persistence;
using Hris.AuthService.Infrastructure.Repositories;
using Hris.AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Hris.AuthService.Api.Configuration;

public static class ServiceRegistration
{
    public static void AddApiServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers();

        // Database - Build connection string from environment variables
        var dbHost = config["DB_HOST"];
        var dbPort = config["DB_PORT"];
        var dbName = config["DB_NAME"];
        var dbUser = config["DB_USER"];
        var dbPassword = config["DB_PASSWORD"];

        var connectionString =
            $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};";

        services.AddDbContext<AuthDbContext>(opt =>
            opt.UseNpgsql(connectionString)
               .UseSnakeCaseNamingConvention()
        );

        // JWT options - Validate key is set
        var jwtKey = config["JWT_KEY"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT_KEY is not configured. Ensure JWT_KEY is set in your .env file and properly loaded.");
        }

        var jwt = new JwtOptions
        {
            Issuer = config["JWT_ISSUER"] ?? "Hris.AuthService",
            Audience = config["JWT_AUDIENCE"] ?? "Hris.Client",
            Key = jwtKey,
            AccessTokenMinutes = int.TryParse(config["JWT_ACCESS_MINUTES"], out var m) ? m : 15,
            RefreshTokenDays = int.TryParse(config["JWT_REFRESH_DAYS"], out var d) ? d : 14
        };

        services.AddSingleton(jwt);
        services.AddSingleton<IJwtOptions>(jwt);

        // ===== Application/Infrastructure bindings =====
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();

        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<CreateUserHandler>();
        // ==============================================

        // ✅ JWT events (401/403 JSON + logging)
        services.AddScoped<JwtLoggingEvents>();

        // ✅ Redis
        var redisHost = config["REDIS_HOST"] ?? "redis";
        var redisPort = config["REDIS_PORT"] ?? "6379";
        var redisConn = $"{redisHost}:{redisPort}";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConn)
        );

        // ✅ Login throttling (Application interface + Infrastructure implementation)
        services.AddScoped<ILoginThrottler, RedisLoginThrottler>();

        // AuthN
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.Zero
                };

                o.IncludeErrorDetails = false;
                o.EventsType = typeof(JwtLoggingEvents);
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
        });
    }
}
