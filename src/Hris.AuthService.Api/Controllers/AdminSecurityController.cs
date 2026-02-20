using Hris.AuthService.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hris.AuthService.Api.Controllers;

[ApiController]
[Route("admin/security")]
[Authorize(Policy = "AdminOnly")]
public class AdminSecurityController : ControllerBase
{
    private readonly ILoginThrottler _throttler;

    public AdminSecurityController(ILoginThrottler throttler)
    {
        _throttler = throttler;
    }

    public record UnlockLoginRequest(string CompanyCode, string Username);

    /// <summary>
    /// Unlocks a user's login (clears Redis lock + failure count + escalation level).
    /// </summary>
    [HttpPost("unlock-login")]
    public async Task<IActionResult> UnlockLogin([FromBody] UnlockLoginRequest req, CancellationToken ct)
    {
        await _throttler.ClearAsync(req.CompanyCode, req.Username, null, ct);
        return NoContent(); // 204
    }

    /// <summary>
    /// Checks lock status for a user and returns how long until unlock.
    /// </summary>
    [HttpGet("login-lock-status")]
    public async Task<IActionResult> GetLoginLockStatus([FromQuery] string companyCode, [FromQuery] string username, CancellationToken ct)
    {
        var (isLocked, retryAfterSeconds) = await _throttler.IsLockedAsync(companyCode, username, null, ct);

        if (!isLocked)
        {
            return Ok(new
            {
                isLocked = false,
                retryAfterSeconds = 0,
                retryAfter = "00:00",
                lockUntilUtc = (string?)null
            });
        }

        var lockUntilUtc = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds);

        return Ok(new
        {
            isLocked = true,
            retryAfterSeconds,
            retryAfter = $"{retryAfterSeconds / 60:D2}:{retryAfterSeconds % 60:D2}", // e.g. 30:00
            lockUntilUtc = lockUntilUtc.ToString("O")
        });
    }
}
