# AuthService

dotnet add src/Hris.AuthService.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/Hris.AuthService.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/Hris.AuthService.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Hris.AuthService.Infrastructure package Microsoft.AspNetCore.Identity

dotnet add src/Hris.AuthService.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Hris.AuthService.Api package Swashbuckle.AspNetCore
dotnet add src/Hris.AuthService.Api package dotenv.net

dotnet tool install --global dotnet-ef


dotnet ef migrations add InitialAuthSchema \
  --project src/Hris.AuthService.Infrastructure \
  --startup-project src/Hris.AuthService.Api

  dotnet ef database update \
  --project src/Hris.AuthService.Infrastructure \
  --startup-project src/Hris.AuthService.Api

