using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/rfid-tags")]
public sealed class RfidTagsController(IAssetDataService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<IdentifierDto>>>> List(
        [FromQuery] ListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<IdentifierDto>>.Ok(await service.ListRfidTagsAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.GetRfidTagAsync(id, cancellationToken)));

    [HttpGet("exists")]
    public async Task<ActionResult<ApiResponse<bool>>> Exists(
        [FromQuery] string value, CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(await service.RfidExistsAsync(value, cancellationToken)));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Create(
        RfidTagRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateRfidTagAsync(request, cancellationToken);
        return Created($"{Request.Path}/{result.Id}", ApiResponse<IdentifierDto>.Ok(result, "RFID tag created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Update(
        Guid id, RfidTagRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.UpdateRfidTagAsync(id, request, cancellationToken)));

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Assign(
        Guid id, AssetAssignmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.AssignRfidTagAsync(id, request.AssetId, cancellationToken),
            "RFID tag assigned successfully."));

    [HttpPost("{id:guid}/unassign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Unassign(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.UnassignRfidTagAsync(id, cancellationToken),
            "RFID tag unassigned successfully."));

    [HttpPost("{id:guid}/replace")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Replace(
        Guid id, ReplacementRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(
            await service.ReplaceRfidTagAsync(id, request.ReplacementId, cancellationToken),
            "RFID tag replaced and the previous tag retired."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateRfidTagAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IdentifierDto>>> Restore(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IdentifierDto>.Ok(await service.RestoreRfidTagAsync(id, cancellationToken),
            "RFID tag restored successfully."));
}
