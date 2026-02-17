using System.Security.Claims;
using System.Text;
using Hris.AuthService.Infrastructure.Persistence;
using Hris.AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Hris.AuthService.Api.Configuration;

public static class ServiceRegistration
{
    public static void AddApiServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers();

        // Database
        services.AddDbContext<AuthDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("AuthDb"))
        );

        // JWT options
        var jwt = new JwtOptions
        {
            Issuer = config["Jwt:Issuer"] ?? "",
            Audience = config["Jwt:Audience"] ?? "",
            Key = config["Jwt:Key"] ?? "",
            AccessTokenMinutes = int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 15,
            RefreshTokenDays = int.TryParse(config["Jwt:RefreshTokenDays"], out var d) ? d : 14
        };
        services.AddSingleton(jwt);

        // Token + password services
        services.AddSingleton<TokenService>();
        services.AddSingleton<PasswordHasherAdapter>();

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
                    RoleClaimType = ClaimTypes.Role
                };
            });

        // AuthZ
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
        });
    }
}
