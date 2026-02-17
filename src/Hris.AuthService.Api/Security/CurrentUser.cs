using System.Security.Claims;

namespace Hris.AuthService.Api.Security;

public static class CurrentUser
{
    public static Guid UserId(ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? throw new Exception("No user id"));

    public static Guid CompanyId(ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue("company_id") ?? throw new Exception("No company_id claim"));
}
