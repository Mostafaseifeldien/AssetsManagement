using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssetsManagement.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AssetsManagement.Infrastructure;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options) : IAuthService
{
    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return null;

        var roles = await userManager.GetRolesAsync(user);
        var expiry = DateTime.UtcNow.AddMinutes(options.Value.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(ClaimTypes.Name, user.UserName!),
            new("display_name", user.DisplayName ?? user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Value.Issuer, options.Value.Audience, claims,
            expires: expiry, signingCredentials: credentials);
        return new(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expiry, user.UserName!, roles.ToArray());
    }
}
