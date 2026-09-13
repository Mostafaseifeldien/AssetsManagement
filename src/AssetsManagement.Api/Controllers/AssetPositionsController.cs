using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/asset-positions")]
public sealed class AssetPositionsController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetPositionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetPositionDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetPositionDto>>.Ok(
            await service.ListPositionsAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetPositionDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetPositionDto>.Ok(
            await service.GetPositionAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetPositionDto>>> Create(
        AssetPositionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreatePositionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetPositionDto>.Ok(result, "Position recorded successfully."));
    }
}
