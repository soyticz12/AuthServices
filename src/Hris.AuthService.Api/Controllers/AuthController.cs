using Hris.AuthService.Application.Abstractions;
using Hris.AuthService.Application.Auth.Login;
using Hris.AuthService.Application.Auth.Refresh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hris.AuthService.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _login;
    private readonly RefreshHandler _refresh;
    private readonly ILoginThrottler _throttler;

    public AuthController(LoginHandler login, RefreshHandler refresh, ILoginThrottler throttler)
    {
        _login = login;
        _refresh = refresh;
        _throttler = throttler;
    }

    private static string FormatMinSec(int totalSeconds)
    {
        var m = totalSeconds / 60;
        var s = totalSeconds % 60;
        return $"{m:D2}:{s:D2}";
    }

    public record LoginRequest(string Username, string Password, string CompanyCode);

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        // ✅ Check lock
        var (locked, retryAfterSeconds) = await _throttler.IsLockedAsync(req.CompanyCode, req.Username, ip, ct);
        if (locked)
        {
            var lockUntilUtc = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds);

            Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

            var pd = new ProblemDetails
            {
                Title = "Too Many Requests",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = $"Too many failed login attempts. Try again at {lockUntilUtc:O} (UTC)."
            };

            pd.Extensions["retryAfterSeconds"] = retryAfterSeconds;
            pd.Extensions["retryAfter"] = FormatMinSec(retryAfterSeconds); // ✅ not "time"
            pd.Extensions["lockUntilUtc"] = lockUntilUtc.ToString("O");

            return StatusCode(StatusCodes.Status429TooManyRequests, pd);
        }

        var result = await _login.Handle(new LoginCommand(req.Username, req.Password, req.CompanyCode), ip, ua, ct);

        if (result.IsSuccess)
        {
            await _throttler.ClearAsync(req.CompanyCode, req.Username, ip, ct); // ✅ reset lock level
            return Ok(result.Value);
        }

        // ✅ Count only wrong password (usually 401)
        if (result.StatusCode == StatusCodes.Status401Unauthorized)
        {
            var (lockedNow, lockSeconds, attempts) =
                await _throttler.RegisterFailureAsync(req.CompanyCode, req.Username, ip, ct);

            if (lockedNow)
            {
                var lockUntilUtc = DateTimeOffset.UtcNow.AddSeconds(lockSeconds);
                Response.Headers["Retry-After"] = lockSeconds.ToString();

                var pd = new ProblemDetails
                {
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = $"Too many failed login attempts. Try again at {lockUntilUtc:O} (UTC)."
                };

                pd.Extensions["retryAfterSeconds"] = lockSeconds;
                pd.Extensions["retryAfter"] = FormatMinSec(lockSeconds);
                pd.Extensions["lockUntilUtc"] = lockUntilUtc.ToString("O");

                return StatusCode(StatusCodes.Status429TooManyRequests, pd);
            }

            // Optional: you can include attempts if you want
            // return StatusCode(401, new { error = result.Error, attempts });

            return StatusCode(result.StatusCode, result.Error);
        }

        return StatusCode(result.StatusCode, result.Error);
    }

    public record RefreshRequest(string RefreshToken);

    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        var result = await _refresh.Handle(new RefreshCommand(req.RefreshToken), ip, ua, ct);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result.Error);

        return Ok(result.Value);
    }
}
