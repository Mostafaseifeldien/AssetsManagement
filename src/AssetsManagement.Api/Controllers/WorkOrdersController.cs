using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
public sealed class WorkOrdersController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<WorkOrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkOrderListItemDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<WorkOrderListItemDto>>.Ok(
            await service.ListWorkOrdersAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkOrderDetailDto>.Ok(
            await service.GetWorkOrderAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> Create(
        WorkOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateWorkOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<WorkOrderDetailDto>.Ok(result, "Work order created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> Update(
        Guid id, WorkOrderRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkOrderDetailDto>.Ok(
            await service.UpdateWorkOrderAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> Start(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkOrderDetailDto>.Ok(
            await service.StartWorkOrderAsync(id, cancellationToken), "Work order started."));

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> Complete(
        Guid id, WorkOrderCompleteRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkOrderDetailDto>.Ok(
            await service.CompleteWorkOrderAsync(id, request, cancellationToken), "Work order completed."));

    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> Verify(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkOrderDetailDto>.Ok(
            await service.VerifyWorkOrderAsync(id, cancellationToken), "Work order verified."));

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkOrderDetailDto>>> AddLine(
        Guid id, WorkOrderLineRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkOrderDetailDto>.Ok(
            await service.AddWorkOrderLineAsync(id, request, cancellationToken), "Line added."));
}
