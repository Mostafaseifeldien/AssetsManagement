using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController(IAssetService service, IAssetOperationsService operations) : ControllerBase
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

    [HttpGet("specification")]
    public async Task<ActionResult<ApiResponse<AssetSpecificationDto>>> Specification() =>
        Ok(ApiResponse<AssetSpecificationDto>.Ok(
            await operations.GetSpecificationAsync(),
            "Specification retrieved successfully."));

    [HttpGet("{id:guid}/screen")]
    public async Task<ActionResult<ApiResponse<AssetScreenDto>>> Screen(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetScreenDto>.Ok(
            await operations.GetScreenAsync(id, cancellationToken), "Asset screen retrieved successfully."));

    [HttpGet("{id:guid}/financials")]
    public async Task<ActionResult<ApiResponse<AssetFinancialsDto>>> Financials(
        Guid id, CancellationToken cancellationToken) =>
            Ok(ApiResponse<AssetFinancialsDto>.Ok(
            await operations.GetFinancialsAsync(id, cancellationToken), "Cost and warranty retrieved successfully."));

    [HttpGet("{id:guid}/depreciation")]
    public async Task<ActionResult<ApiResponse<DepreciationScheduleDetailDto>>> Depreciation(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DepreciationScheduleDetailDto>.Ok(
            await operations.GetAssetScheduleAsync(id, cancellationToken), "Depreciation schedule retrieved successfully."));

    [HttpGet("{id:guid}/maintenance")]
    public async Task<ActionResult<ApiResponse<AssetMaintenanceDto>>> Maintenance(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetMaintenanceDto>.Ok(
            await operations.GetMaintenanceAsync(id, cancellationToken), "Maintenance retrieved successfully."));

    [HttpGet("{id:guid}/identity")]
    public async Task<ActionResult<ApiResponse<AssetIdentityDto>>> Identity(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetIdentityDto>.Ok(
            await operations.GetIdentityAsync(id, cancellationToken), "Identity and tags retrieved successfully."));

    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<ApiResponse<AssetMovementDto>>> Movements(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetMovementDto>.Ok(
            await operations.GetMovementsAsync(id, cancellationToken), "Movement history retrieved successfully."));

    [HttpGet("{id:guid}/map-position")]
    public async Task<ActionResult<ApiResponse<AssetPositionDto?>>> MapPosition(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetPositionDto?>.Ok(
            await operations.GetMapPositionAsync(id, cancellationToken), "Map position retrieved successfully."));

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
