# ✅ Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# ✅ Clear Windows NuGet fallback folders — prevents "C:\Program Files\..." errors
ENV NUGET_FALLBACK_PACKAGES=""

COPY src/Hris.AuthService.Api/Hris.AuthService.Api.csproj                           src/Hris.AuthService.Api/
COPY src/Hris.AuthService.Application/Hris.AuthService.Application.csproj           src/Hris.AuthService.Application/
COPY src/Hris.AuthService.Infrastructure/Hris.AuthService.Infrastructure.csproj     src/Hris.AuthService.Infrastructure/
COPY src/Hris.AuthService.Domain/Hris.AuthService.Domain.csproj                     src/Hris.AuthService.Domain/

RUN dotnet restore src/Hris.AuthService.Api/Hris.AuthService.Api.csproj

# ✅ Stage 2: Publish
COPY . .
RUN dotnet publish src/Hris.AuthService.Api/Hris.AuthService.Api.csproj \
    -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ✅ Stage 3: Runtime only
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hris.AuthService.Api.dll"]