using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/asset-types")]
public sealed class AssetTypesController(IAssetTypeService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetTypeListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetTypeListItemDto>>>> List(
        [FromQuery] AssetTypeListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetTypeListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetTypeDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetTypeHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetTypeHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Create(
        AssetTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetTypeDetailDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Update(
        Guid id, AssetTypeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetTypeDetailDto>.Ok(
            await service.UpdateAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetTypeDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}
