using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hris.AuthService.Api.Security;

public sealed class JwtLoggingEvents : JwtBearerEvents
{
    private readonly ILogger<JwtLoggingEvents> _logger;

    public JwtLoggingEvents(ILogger<JwtLoggingEvents> logger)
    {
        _logger = logger;
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        _logger.LogWarning(context.Exception,
            "JWT authentication failed. Path={Path} IP={IP} UA={UA}",
            context.Request.Path.Value,
            context.HttpContext.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString());

        // Let Challenge generate the 401 body (so we don't double-write)
        return Task.CompletedTask;
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        // Missing token OR invalid token -> challenge
        _logger.LogInformation(
            "JWT challenge. Error={Error} Desc={Desc} Path={Path} IP={IP}",
            context.Error,
            context.ErrorDescription,
            context.Request.Path.Value,
            context.HttpContext.Connection.RemoteIpAddress?.ToString());

        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "Missing, invalid, or expired access token."
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    public override Task Forbidden(ForbiddenContext context)
    {
        _logger.LogWarning(
            "JWT forbidden. Path={Path} IP={IP} User={User}",
            context.HttpContext.Request.Path.Value,
            context.HttpContext.Connection.RemoteIpAddress?.ToString(),
            context.HttpContext.User?.Identity?.Name ?? "(no name)");

        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = "You do not have permission to access this resource."
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
