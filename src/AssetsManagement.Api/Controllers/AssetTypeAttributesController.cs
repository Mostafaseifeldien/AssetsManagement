using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/asset-type-attributes")]
public sealed class AssetTypeAttributesController(IAssetTypeAttributeService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TypeAttributeGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TypeAttributeGroupDto>>>> List(
        [FromQuery] TypeAttributeListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<TypeAttributeGroupDto>>.Ok(
            await service.ListGroupedAsync(query, cancellationToken)));

    [HttpGet("grouped")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TypeAttributeGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TypeAttributeGroupDto>>>> Grouped(
        [FromQuery] TypeAttributeListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<TypeAttributeGroupDto>>.Ok(
            await service.ListGroupedAsync(query, cancellationToken)));

    [HttpGet("types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TypeAttributeTypeOptionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TypeAttributeTypeOptionDto>>>> Types(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<TypeAttributeTypeOptionDto>>.Ok(
            await service.ListTypesAsync(cancellationToken)));

    [HttpGet("exists")]
    public async Task<ActionResult<ApiResponse<bool>>> Exists(
        [FromQuery] string code, [FromQuery] string? assetType, [FromQuery] Guid? excludingId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(await service.ExistsAsync(code, assetType, excludingId, cancellationToken)));

    [HttpGet("/api/asset-types/{assetTypeId:guid}/attributes")]
    public async Task<ActionResult<ApiResponse<PagedResult<TypeAttributeListItemDto>>>> ListForType(
        Guid assetTypeId, [FromQuery] TypeAttributeListQuery query, CancellationToken cancellationToken)
    {
        var scoped = new TypeAttributeListQuery
        {
            PageNumber = query.PageNumber, PageSize = query.PageSize, Search = query.Search,
            SortBy = query.SortBy, SortDirection = query.SortDirection, Active = query.Active,
            AssetTypeId = assetTypeId, DataType = query.DataType, Class = query.Class,
            Requirement = query.Requirement, ShowInList = query.ShowInList
        };
        return Ok(ApiResponse<PagedResult<TypeAttributeListItemDto>>.Ok(
            await service.ListAsync(scoped, cancellationToken)));
    }

    [HttpPost("/api/asset-types/{assetTypeId:guid}/attributes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDetailDto>>> CreateField(
        Guid assetTypeId, TypeAttributeFieldRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateFieldAsync(assetTypeId, request, cancellationToken);
        return Created($"/api/asset-type-attributes/{result.Id}",
            ApiResponse<TypeAttributeDetailDto>.Ok(result, "Attribute created and assigned successfully."));
    }

    [HttpPut("/api/asset-types/{assetTypeId:guid}/attributes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDetailDto>>> UpdateFieldForType(
        Guid assetTypeId, Guid id, TypeAttributeFieldRequest request, CancellationToken cancellationToken)
    {
        var scoped = new TypeAttributeFieldRequest
        {
            AssetType = request.AssetType ?? assetTypeId.ToString(),
            Code = request.Code, Label = request.Label, AlternateName = request.AlternateName,
            DataType = request.DataType, PossibleValues = request.PossibleValues, Unit = request.Unit,
            Class = request.Class, Requirement = request.Requirement, DisplayOrder = request.DisplayOrder,
            ShowInList = request.ShowInList, HelpText = request.HelpText
        };
        return Ok(ApiResponse<TypeAttributeDetailDto>.Ok(
            await service.UpdateFieldAsync(id, scoped, cancellationToken), "Type attribute updated successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TypeAttributeDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDetailDto>>> Create(
        TypeAttributeFieldRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateFieldAsync(null, request, cancellationToken);
        return Created($"/api/asset-type-attributes/{result.Id}",
            ApiResponse<TypeAttributeDetailDto>.Ok(result, "Attribute created and assigned successfully."));
    }

    [HttpPost("fields")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDetailDto>>> DefineField(
        TypeAttributeFieldRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateFieldAsync(null, request, cancellationToken);
        return Created($"/api/asset-type-attributes/{result.Id}",
            ApiResponse<TypeAttributeDetailDto>.Ok(result, "Attribute created and assigned successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDetailDto>>> Update(
        Guid id, TypeAttributeFieldRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TypeAttributeDetailDto>.Ok(
            await service.UpdateFieldAsync(id, request, cancellationToken), "Type attribute updated successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TypeAttributeDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TypeAttributeDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}
