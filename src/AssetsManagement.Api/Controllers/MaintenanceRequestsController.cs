using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/maintenance-requests")]
public sealed class MaintenanceRequestsController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MaintenanceRequestListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<MaintenanceRequestListItemDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<MaintenanceRequestListItemDto>>.Ok(
            await service.ListRequestsAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaintenanceRequestDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MaintenanceRequestDetailDto>.Ok(
            await service.GetRequestAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MaintenanceRequestDetailDto>>> Create(
        MaintenanceRequestCreate request, CancellationToken cancellationToken)
    {
        var result = await service.CreateRequestAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<MaintenanceRequestDetailDto>.Ok(result, "Fault reported successfully."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MaintenanceRequestDetailDto>>> Reject(
        Guid id, MaintenanceRejectRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MaintenanceRequestDetailDto>.Ok(
            await service.RejectRequestAsync(id, request, cancellationToken), "Request rejected."));

    [HttpPost("{id:guid}/convert")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> Convert(
        Guid id, MaintenanceConvertRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkOrderDetailDto>.Ok(
            await service.ConvertRequestAsync(id, request, cancellationToken),
            "Request converted to a work order."));
}
