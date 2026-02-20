using Hris.AuthService.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Hris.AuthService.Api.Middleware;

public sealed class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, ITokenBlacklist blacklist)
    {
        var raw = ctx.Request.Headers.Authorization.ToString();

        if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = raw["Bearer ".Length..].Trim();
            var handler = new JwtSecurityTokenHandler();

            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                if (!string.IsNullOrEmpty(jwt.Id) && await blacklist.IsRevokedAsync(jwt.Id, ctx.RequestAborted))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "application/problem+json";

                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Unauthorized",
                        Detail = "Your session has expired. Please log in again."
                    };

                    await ctx.Response.WriteAsJsonAsync(problem);
                    return;
                }
            }
        }

        await _next(ctx);
    }
}