using System.Diagnostics;
using System.Text.Json;
using System.Threading.RateLimiting;
using dotenv.net;
using Hris.AuthService.Api.Configuration;
using Hris.AuthService.Api.Seeding;
using Microsoft.AspNetCore.Mvc;

var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Looking for .env at: {envFile}");
Console.WriteLine($".env file exists: {File.Exists(envFile)}");

if (File.Exists(envFile))
{
    DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envFile }));

    var envVars = DotEnv.Read(new DotEnvOptions(envFilePaths: new[] { envFile }));
    foreach (var kvp in envVars)
    {
        Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        Console.WriteLine($"Loaded env var: {kvp.Key}");
    }
}
else
{
    Console.WriteLine("No .env file found, relying on system/container environment variables.");
}

Console.WriteLine($"JWT_KEY loaded: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_KEY"))}");

var builder = WebApplication.CreateBuilder(args);

// ✅ MUST be before AddApiServices so config["JWT_KEY"] is populated
builder.Configuration.AddEnvironmentVariables();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger (no OpenApi models)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Optional: return JSON body for 429
    options.OnRejected = async (context, _) =>
    {
        if (!context.HttpContext.Response.HasStarted)
        {
            context.HttpContext.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Rate limit exceeded. Please try again later."
            };

            await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    };

    // 5/min per IP for login
    options.AddPolicy("login", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    // 20/min per IP for refresh
    options.AddPolicy("refresh", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

// Your registrations (DbContext, handlers, auth, etc.)
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// ✅ Global exception handler → JSON for 500
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            status = 500,
            title = "Server error",
            detail = "An unexpected error occurred."
        });
    });
});

app.UseCors("AllowAll");

// ✅ Request logging (logs 200/401/403/429/500 etc.)
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("HTTP");

    var ip = context.Connection.RemoteIpAddress?.ToString();
    var ua = context.Request.Headers.UserAgent.ToString();

    logger.LogInformation(
        "{Time} {Method} {Path} -> {Status} ({ElapsedMs}ms) IP={IP} UA={UA}",
        DateTimeOffset.UtcNow,
        context.Request.Method,
        context.Request.Path.Value,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds,
        ip,
        ua);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Enable rate limiting middleware (put before auth/authorization)
app.UseRateLimiter();

// If you're calling via HTTP (like localhost:8080), HTTPS redirection can confuse clients.
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await SeedData.EnsureSeededAsync(app);

app.Run();
