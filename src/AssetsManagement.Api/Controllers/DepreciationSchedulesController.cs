using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/depreciation-schedules")]
public sealed class DepreciationSchedulesController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DepreciationScheduleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DepreciationScheduleListItemDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<DepreciationScheduleListItemDto>>.Ok(
            await service.ListSchedulesAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepreciationScheduleDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DepreciationScheduleDetailDto>.Ok(
            await service.GetScheduleAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<DepreciationScheduleDetailDto>>> Create(
        DepreciationScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateScheduleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<DepreciationScheduleDetailDto>.Ok(result, "Depreciation schedule created successfully."));
    }

    [HttpPost("{id:guid}/supersede")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<DepreciationScheduleDetailDto>>> Supersede(
        Guid id, DepreciationScheduleRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DepreciationScheduleDetailDto>.Ok(
            await service.SupersedeScheduleAsync(id, request, cancellationToken),
            "Schedule superseded successfully."));
}
