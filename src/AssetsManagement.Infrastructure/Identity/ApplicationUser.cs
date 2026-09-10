using Microsoft.AspNetCore.Identity;

namespace AssetsManagement.Infrastructure;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
