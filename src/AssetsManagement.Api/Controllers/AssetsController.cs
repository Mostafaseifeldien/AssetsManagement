using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController(IAssetService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetListItemDto>>>> List(
        [FromQuery] AssetListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("missing")]
    [ProducesResponseType(typeof(ApiResponse<MissingAssetsResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MissingAssetsResult>>> Missing(
        [FromQuery] MissingAssetQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MissingAssetsResult>.Ok(
            await service.ListMissingAsync(query, cancellationToken)));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetLookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetLookupDto>>.Ok(
            await service.LookupAsync(search, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDetailDto>>> Create(
        AssetRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetDetailDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDetailDto>>> Update(
        Guid id, AssetRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDetailDto>.Ok(
            await service.UpdateAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDetailDto>>> ChangeStatus(
        Guid id, AssetStatusChangeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDetailDto>.Ok(
            await service.ChangeStatusAsync(id, request, cancellationToken), "Status updated successfully."));

    [HttpPost("{id:guid}/location")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDetailDto>>> UpdateLocation(
        Guid id, AssetLocationRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDetailDto>.Ok(
            await service.UpdateLocationAsync(id, request, cancellationToken), "Location updated successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}
