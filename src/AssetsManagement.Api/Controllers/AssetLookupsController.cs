using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetLookupsController(IAssetDataService service) : ControllerBase
{
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetLookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetLookupDto>>.Ok(
            await service.AssetLookupAsync(search, cancellationToken)));
}
