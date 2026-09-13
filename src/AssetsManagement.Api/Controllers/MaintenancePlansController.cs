using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/maintenance-plans")]
public sealed class MaintenancePlansController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MaintenancePlanDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<MaintenancePlanDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<MaintenancePlanDto>>.Ok(
            await service.ListPlansAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaintenancePlanDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MaintenancePlanDto>.Ok(
            await service.GetPlanAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MaintenancePlanDto>>> Create(
        MaintenancePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreatePlanAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<MaintenancePlanDto>.Ok(result, "Maintenance plan created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MaintenancePlanDto>>> Update(
        Guid id, MaintenancePlanRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MaintenancePlanDto>.Ok(
            await service.UpdatePlanAsync(id, request, cancellationToken), "Record updated successfully."));
}
