using System.Security.Claims;
using AssetsManagement.Application;
using Microsoft.AspNetCore.Http;

namespace AssetsManagement.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string UserName => accessor.HttpContext?.User.Identity?.Name ?? "system";
    public string DisplayName =>
        accessor.HttpContext?.User.FindFirst("display_name")?.Value
        ?? accessor.HttpContext?.User.FindFirst(ClaimTypes.GivenName)?.Value
        ?? UserName;
}
