using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/barcodes")]
public sealed class BarcodesController(IBarcodeService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BarcodeListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BarcodeListItemDto>>>> List(
        [FromQuery] BarcodeListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<BarcodeListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("assets-waiting")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BarcodeWaitingAssetDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BarcodeWaitingAssetDto>>>> AssetsWaiting(
        [FromQuery] BarcodeListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<BarcodeWaitingAssetDto>>.Ok(
            await service.ListAssetsWaitingAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BarcodeDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, status, cancellationToken)));

    [HttpGet("exists")]
    public async Task<ActionResult<ApiResponse<bool>>> Exists(
        [FromQuery] string value, [FromQuery] string? symbology, [FromQuery] Guid? excludingId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(await service.ExistsAsync(value, symbology, excludingId, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BarcodeHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<BarcodeHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Create(
        BarcodeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<BarcodeDetailDto>.Ok(result, "Barcode created successfully."));
    }

    [HttpPost("generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Generate(
        [FromQuery] string symbology = "Code128", CancellationToken cancellationToken = default)
    {
        var result = await service.GenerateAsync(symbology, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<BarcodeDetailDto>.Ok(result, "Barcode generated successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Update(
        Guid id, BarcodeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BarcodeDetailDto>.Ok(
            await service.UpdateAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Assign(
        Guid id, AssetAssignmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BarcodeDetailDto>.Ok(
            await service.AssignAsync(id, request.AssetId, cancellationToken),
            "Barcode assigned successfully."));

    [HttpPost("{id:guid}/unassign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Unassign(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BarcodeDetailDto>.Ok(
            await service.UnassignAsync(id, cancellationToken), "Barcode unassigned successfully."));

    [HttpPost("{id:guid}/replace")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Replace(
        Guid id, ReplacementRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BarcodeDetailDto>.Ok(
            await service.ReplaceAsync(id, request.ReplacementId, cancellationToken),
            "Barcode replaced and the previous barcode kept for historical reads."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BarcodeDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BarcodeDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}
