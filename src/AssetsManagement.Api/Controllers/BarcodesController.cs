using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/barcodes")]
public sealed class BarcodesController(IAssetDataService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<IdentifierDto>>>> List(
        [FromQuery] ListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<IdentifierDto>>.Ok(await service.ListBarcodesAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.GetBarcodeAsync(id, cancellationToken)));

    [HttpGet("exists")]
    public async Task<ActionResult<ApiResponse<bool>>> Exists(
        [FromQuery] string value, CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(await service.BarcodeExistsAsync(value, cancellationToken)));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Create(
        BarcodeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateBarcodeAsync(request, cancellationToken);
        return Created($"{Request.Path}/{result.Id}", ApiResponse<IdentifierDto>.Ok(result, "Barcode created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Update(
        Guid id, BarcodeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.UpdateBarcodeAsync(id, request, cancellationToken)));

    [HttpPost("generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Generate(
        [FromQuery] string symbology = "Code128", CancellationToken cancellationToken = default)
    {
        var result = await service.GenerateBarcodeAsync(symbology, cancellationToken);
        return Created($"/api/barcodes/{result.Id}",
            ApiResponse<IdentifierDto>.Ok(result, "Barcode generated successfully."));
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Assign(
        Guid id, AssetAssignmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.AssignBarcodeAsync(id, request.AssetId, cancellationToken),
            "Barcode assigned successfully."));

    [HttpPost("{id:guid}/unassign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Unassign(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.UnassignBarcodeAsync(id, cancellationToken),
            "Barcode unassigned successfully."));

    [HttpPost("{id:guid}/replace")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Replace(
        Guid id, ReplacementRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(
            await service.ReplaceBarcodeAsync(id, request.ReplacementId, cancellationToken),
            "Barcode replaced and the previous barcode retired."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateBarcodeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Restore(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.RestoreBarcodeAsync(id, cancellationToken),
            "Barcode restored successfully."));
}
