using dotenv.net;

namespace Hris.AuthService.Api.Configuration;

public static class EnvConfiguration
{
    public static void AddEnv(this WebApplicationBuilder builder)
    {
        DotEnv.Load(new DotEnvOptions(
            envFilePaths: new[]
            {
                Path.Combine(builder.Environment.ContentRootPath, ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env")
            },
            ignoreExceptions: true
        ));

        var dbHost = Get("DB_HOST", "localhost");
        var dbPort = Get("DB_PORT", "5432");
        var dbName = Get("DB_NAME", "hris_auth");
        var dbUser = Get("DB_USER", "hris");
        var dbPass = Get("DB_PASSWORD", "hris123");

        builder.Configuration["ConnectionStrings:AuthDb"] =
            $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

        builder.Configuration["Jwt:Issuer"] = Get("JWT_ISSUER", "Hris.AuthService");
        builder.Configuration["Jwt:Audience"] = Get("JWT_AUDIENCE", "Hris.Client");
        builder.Configuration["Jwt:Key"] = Get("JWT_KEY", "");
        builder.Configuration["Jwt:AccessTokenMinutes"] = Get("JWT_ACCESS_MINUTES", "15");
        builder.Configuration["Jwt:RefreshTokenDays"] = Get("JWT_REFRESH_DAYS", "14");
    }

    private static string Get(string key, string fallback)
        => Environment.GetEnvironmentVariable(key) ?? fallback;
}
