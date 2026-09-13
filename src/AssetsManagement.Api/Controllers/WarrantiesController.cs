using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/warranties")]
public sealed class WarrantiesController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<WarrantyListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<WarrantyListItemDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<WarrantyListItemDto>>.Ok(
            await service.ListWarrantiesAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarrantyDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WarrantyDetailDto>.Ok(
            await service.GetWarrantyAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarrantyDetailDto>>> Create(
        WarrantyRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateWarrantyAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<WarrantyDetailDto>.Ok(result, "Warranty recorded successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarrantyDetailDto>>> Update(
        Guid id, WarrantyRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WarrantyDetailDto>.Ok(
            await service.UpdateWarrantyAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarrantyDetailDto>>> Void(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WarrantyDetailDto>.Ok(
            await service.VoidWarrantyAsync(id, cancellationToken), "Warranty voided successfully."));
}
