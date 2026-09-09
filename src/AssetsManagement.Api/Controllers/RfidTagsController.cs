using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/rfid-tags")]
public sealed class RfidTagsController(IRfidTagService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RfidTagListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<RfidTagListItemDto>>>> List(
        [FromQuery] RfidTagListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<RfidTagListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("assets-waiting")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RfidWaitingAssetDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<RfidWaitingAssetDto>>>> AssetsWaiting(
        [FromQuery] RfidTagListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<RfidWaitingAssetDto>>.Ok(
            await service.ListAssetsWaitingAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RfidTagDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RfidTagDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, status, cancellationToken)));

    [HttpGet("exists")]
    public async Task<ActionResult<ApiResponse<bool>>> Exists(
        [FromQuery] string tagIdentifier, [FromQuery] Guid? excludingId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(await service.ExistsAsync(tagIdentifier, excludingId, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RfidTagHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<RfidTagHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RfidTagDetailDto>>> Create(
        RfidTagRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<RfidTagDetailDto>.Ok(result, "RFID tag created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RfidTagDetailDto>>> Update(
        Guid id, RfidTagRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RfidTagDetailDto>.Ok(
            await service.UpdateAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RfidTagDetailDto>>> Assign(
        Guid id, AssetAssignmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RfidTagDetailDto>.Ok(
            await service.AssignAsync(id, request.AssetId, cancellationToken),
            "RFID tag assigned successfully."));

    [HttpPost("{id:guid}/unassign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RfidTagDetailDto>>> Unassign(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RfidTagDetailDto>.Ok(
            await service.UnassignAsync(id, cancellationToken), "RFID tag unassigned successfully."));

    [HttpPost("{id:guid}/replace")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RfidTagDetailDto>>> Replace(
        Guid id, ReplacementRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RfidTagDetailDto>.Ok(
            await service.ReplaceAsync(id, request.ReplacementId, cancellationToken),
            "RFID tag replaced and the previous tag kept for historical reads."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RfidTagDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RfidTagDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}
