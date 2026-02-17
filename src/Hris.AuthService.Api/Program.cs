using Hris.AuthService.Api.Configuration;
using Hris.AuthService.Api.Seeding;
using dotenv.net;

// Load .env file
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Looking for .env at: {envFile}");
Console.WriteLine($".env file exists: {File.Exists(envFile)}");

DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envFile }));

var envVars = DotEnv.Read(new DotEnvOptions(envFilePaths: new[] { envFile }));

// Set as environment variables
foreach (var kvp in envVars)
{
    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
    Console.WriteLine($"Loaded env var: {kvp.Key}");
}

Console.WriteLine($"JWT_KEY loaded: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_KEY"))}");

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSwaggerGen();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed admin/company/roles
await SeedData.EnsureSeededAsync(app);

app.Run();