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

[ApiController]
[Route("api/asset-types")]
public sealed class AssetTypesController(IAssetTypeService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetTypeListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetTypeListItemDto>>>> List(
        [FromQuery] AssetTypeListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetTypeListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetTypeDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetTypeHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetTypeHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Create(
        AssetTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetTypeDetailDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Update(
        Guid id, AssetTypeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetTypeDetailDto>.Ok(
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
    public async Task<ActionResult<ApiResponse<AssetTypeDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetTypeDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}

[ApiController]
[Route("api/asset-categories")]
public sealed class AssetCategoriesController(IAssetCategoryService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetCategoryListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetCategoryListItemDto>>>> List(
        [FromQuery] AssetCategoryListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetCategoryListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetCategoryDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetCategoryDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetCategoryHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetCategoryHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetCategoryDetailDto>>> Create(
        AssetCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetCategoryDetailDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetCategoryDetailDto>>> Update(
        Guid id, AssetCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetCategoryDetailDto>.Ok(
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
    public async Task<ActionResult<ApiResponse<AssetCategoryDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetCategoryDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}

[ApiController]
[Route("api/asset-models")]
public sealed class AssetModelsController(IAssetModelService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetModelListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetModelListItemDto>>>> List(
        [FromQuery] AssetModelListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetModelListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetModelDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetModelDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await service.LookupAsync(search, cancellationToken)));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetModelHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetModelHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetModelDetailDto>>> Create(
        AssetModelRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetModelDetailDto>.Ok(result, "Record created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetModelDetailDto>>> Update(
        Guid id, AssetModelRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetModelDetailDto>.Ok(
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
    public async Task<ActionResult<ApiResponse<AssetModelDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetModelDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));
}

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

[Route("api/suppliers")]
public sealed class SuppliersController(IAssetDataService service)
    : MasterDataController(service, AssetDataResource.Suppliers);

[Route("api/asset-statuses")]
public sealed class AssetStatusesController(IAssetDataService service)
    : MasterDataController(service, AssetDataResource.AssetStatuses)
{
    [HttpGet("{id:guid}/allowed-transitions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> AllowedTransitions(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await Service.GetAllowedStatusTransitionsAsync(id, cancellationToken)));

    [HttpPut("{id:guid}/allowed-transitions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> SetAllowedTransitions(
        Guid id, StatusTransitionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await Service.SetAllowedStatusTransitionsAsync(id, request, cancellationToken),
            "Allowed transitions updated successfully."));
}

[Route("api/custom-attribute-definitions")]
public sealed class CustomAttributeDefinitionsController(IAssetDataService service)
    : MasterDataController(service, AssetDataResource.CustomAttributeDefinitions);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<ActionResult<ApiResponse<LoginResult>>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new ApiResponse<object>(false, "Invalid username or password.", null,
                [new ApiError(null, "Invalid username or password.", "invalid_credentials")]));
        return Ok(ApiResponse<LoginResult>.Ok(result, "Login successful."));
    }
}

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

public sealed record AssetAssignmentRequest(Guid AssetId);
public sealed record ReplacementRequest(Guid ReplacementId);

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

public sealed class ImageUploadRequest
{
    public required IFormFile File { get; init; }
    public string? Caption { get; init; }
    public string? Purpose { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed record ImageMetadataRequest(string? Caption, string? Purpose);

[ApiController]
[Route("api/asset-images")]
public sealed class AssetImagesController(IAssetDataService service) : ControllerBase
{
    private const long MaximumImageSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetImageDto>>>> List(
        [FromQuery] ListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetImageDto>>.Ok(await service.ListImagesAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDto>.Ok(await service.GetImageAsync(id, cancellationToken)));

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken cancellationToken)
    {
        var file = await service.OpenImageAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumImageSize)]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> Upload(
        [FromQuery] Guid assetId, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        await ValidateImageAsync(request.File, cancellationToken);
        await using var stream = request.File.OpenReadStream();
        var result = await service.UploadImageAsync(assetId, new ImageUpload
        {
            Content = stream, OriginalFileName = Path.GetFileName(request.File.FileName),
            ContentType = request.File.ContentType, SizeBytes = request.File.Length,
            Caption = request.Caption, Purpose = request.Purpose, IsPrimary = request.IsPrimary
        }, cancellationToken);
        return Created($"/api/asset-images/{result.Id}",
            ApiResponse<AssetImageDto>.Ok(result, "Image uploaded successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> Update(
        Guid id, ImageMetadataRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDto>.Ok(
            await service.UpdateImageAsync(id, request.Caption, request.Purpose, cancellationToken),
            "Image metadata updated successfully."));

    [HttpPost("{id:guid}/primary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> SetPrimary(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDto>.Ok(await service.SetPrimaryImageAsync(id, cancellationToken),
            "Primary image selected successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteImageAsync(id, cancellationToken);
        return NoContent();
    }

    private static async Task ValidateImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaximumImageSize)
            throw new Domain.DomainRuleException("Image size must be between 1 byte and 10 MB.");
        var extension = Path.GetExtension(file.FileName);
        if (!Extensions.Contains(extension) || !ContentTypes.Contains(file.ContentType))
            throw new Domain.DomainRuleException("Only JPEG, PNG and WebP images are supported.");
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header, cancellationToken);
        var isJpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var isWebp = read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                     header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        if (!isJpeg && !isPng && !isWebp)
            throw new Domain.DomainRuleException("The file content does not match a supported image format.");
    }
}

[ApiController]
[Route("api/assets")]
public sealed class AssetLookupsController(IAssetDataService service) : ControllerBase
{
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetLookupDto>>>> Lookup(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetLookupDto>>.Ok(
            await service.AssetLookupAsync(search, cancellationToken)));
}
