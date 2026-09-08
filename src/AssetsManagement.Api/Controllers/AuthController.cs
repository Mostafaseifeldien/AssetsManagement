using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<ActionResult<ApiResponse<LoginResult>>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new ApiResponse<object>(false, "Invalid username or password.", null,
                [new ApiError(null, "Invalid username or password.", "invalid_credentials")]));
        return Ok(ApiResponse<LoginResult>.Ok(result, "Login successful."));
    }
}
