using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
public abstract class MasterDataController(
    IAssetDataService service,
    AssetDataResource resource) : ControllerBase
{
    protected IAssetDataService Service => service;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetDataDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetDataDto>>>> List(
        [FromQuery] ListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetDataDto>>.Ok(
            await service.ListAsync(resource, query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDataDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDataDto>.Ok(await service.GetAsync(resource, id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(resource, search, cancellationToken)));

    [HttpGet("exists")]
    public async Task<ActionResult<ApiResponse<bool>>> Exists(
        [FromQuery] string code, [FromQuery] Guid? excludingId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(await service.ExistsAsync(resource, code, excludingId, cancellationToken)));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDataDto>>> Create(
        MasterDataRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(resource, request, cancellationToken);
        return Created($"{Request.Path}/{result.Id}",
            ApiResponse<AssetDataDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDataDto>>> Update(
        Guid id, MasterDataRequest request, [FromHeader(Name = "If-Match")] string? rowVersion,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDataDto>.Ok(
            await service.UpdateAsync(resource, id, request, rowVersion?.Trim('"'), cancellationToken),
            "Record updated successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(resource, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDataDto>>> Restore(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDataDto>.Ok(
            await service.RestoreAsync(resource, id, cancellationToken), "Record restored successfully."));
}
