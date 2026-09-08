using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/manufacturers")]
public sealed class ManufacturersController(IManufacturerService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ManufacturerListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ManufacturerListItemDto>>>> List(
        [FromQuery] ManufacturerListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<ManufacturerListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ManufacturerDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ManufacturerDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ManufacturerHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<ManufacturerHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ManufacturerDetailDto>>> Create(
        ManufacturerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<ManufacturerDetailDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ManufacturerDetailDto>>> Update(
        Guid id, ManufacturerRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ManufacturerDetailDto>.Ok(
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
    public async Task<ActionResult<ApiResponse<ManufacturerDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ManufacturerDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}
