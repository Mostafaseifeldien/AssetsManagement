using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/asset-type-attributes")]
public sealed class AssetTypeAttributesController(IAssetDataService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TypeAttributeDto>>>> List(
        [FromQuery] ListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<TypeAttributeDto>>.Ok(
            await service.ListTypeAttributesAsync(query, cancellationToken)));

    [HttpGet("/api/asset-types/{assetTypeId:guid}/attributes")]
    public async Task<ActionResult<ApiResponse<PagedResult<TypeAttributeDto>>>> ListForType(
        Guid assetTypeId, [FromQuery] ListQuery query, CancellationToken cancellationToken)
    {
        var scoped = new ListQuery
        {
            PageNumber = query.PageNumber, PageSize = query.PageSize, Search = query.Search,
            SortBy = query.SortBy, SortDirection = query.SortDirection, IsActive = query.IsActive,
            RelatedEntityId = assetTypeId
        };
        return Ok(ApiResponse<PagedResult<TypeAttributeDto>>.Ok(
            await service.ListTypeAttributesAsync(scoped, cancellationToken)));
    }

    [HttpPost("/api/asset-types/{assetTypeId:guid}/attributes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDto>>> CreateDefinition(
        Guid assetTypeId, CreateTypeAttributeDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateTypeAttributeDefinitionAsync(assetTypeId, request, cancellationToken);
        return Created($"/api/asset-type-attributes/{result.Id}",
            ApiResponse<TypeAttributeDto>.Ok(result, "Attribute created and assigned successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TypeAttributeDto>.Ok(await service.GetTypeAttributeAsync(id, cancellationToken)));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDto>>> Create(
        TypeAttributeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateTypeAttributeAsync(request, cancellationToken);
        return Created($"{Request.Path}/{result.Id}",
            ApiResponse<TypeAttributeDto>.Ok(result, "Type attribute assigned successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDto>>> Update(
        Guid id, TypeAttributeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TypeAttributeDto>.Ok(
            await service.UpdateTypeAttributeAsync(id, request, cancellationToken), "Type attribute updated successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateTypeAttributeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDto>>> Restore(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TypeAttributeDto>.Ok(
            await service.RestoreTypeAttributeAsync(id, cancellationToken), "Type attribute restored successfully."));
}
