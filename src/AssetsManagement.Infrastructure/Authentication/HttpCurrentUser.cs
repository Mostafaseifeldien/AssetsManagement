using System.Security.Claims;
using AssetsManagement.Application;
using Microsoft.AspNetCore.Http;

namespace AssetsManagement.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? accessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string UserName => accessor.HttpContext?.User.Identity?.Name ?? "system";
    public string DisplayName =>
        accessor.HttpContext?.User.FindFirst("display_name")?.Value
        ?? accessor.HttpContext?.User.FindFirst(ClaimTypes.GivenName)?.Value
        ?? UserName;
}
