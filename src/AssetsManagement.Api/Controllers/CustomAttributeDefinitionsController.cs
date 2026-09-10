using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/custom-attribute-definitions")]
public sealed class CustomAttributeDefinitionsController(ICustomAttributeDefinitionService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomAttributeDefinitionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomAttributeDefinitionListItemDto>>>> List(
        [FromQuery][Bind(Prefix = "")] CustomAttributeDefinitionListQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<CustomAttributeDefinitionListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomAttributeDefinitionDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustomAttributeDefinitionDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, cancellationToken)));

    [HttpGet("exists")]
    public async Task<ActionResult<ApiResponse<bool>>> Exists(
        [FromQuery] string code, [FromQuery] string? assetType, [FromQuery] Guid? excludingId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(await service.ExistsAsync(code, assetType, excludingId, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CustomAttributeDefinitionHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<CustomAttributeDefinitionHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustomAttributeDefinitionDetailDto>>> Create(
        CustomAttributeDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<CustomAttributeDefinitionDetailDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustomAttributeDefinitionDetailDto>>> Update(
        Guid id, CustomAttributeDefinitionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustomAttributeDefinitionDetailDto>.Ok(
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
    public async Task<ActionResult<ApiResponse<CustomAttributeDefinitionDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustomAttributeDefinitionDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}
