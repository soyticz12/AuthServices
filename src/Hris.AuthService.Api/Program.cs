using Hris.AuthService.Api.Configuration;
using Hris.AuthService.Api.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.AddEnv();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT order
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed admin/company/roles
await SeedData.EnsureSeededAsync(app);

app.Run();
