using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/custody-assignments")]
public sealed class CustodyAssignmentsController(ICustodyService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustodyAssignmentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustodyAssignmentListItemDto>>>> List(
        [FromQuery] CustodyListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<CustodyAssignmentListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustodyAssignmentDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustodyAssignmentDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CustodyHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<CustodyHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustodyAssignmentDetailDto>>> Create(
        CustodyAssignmentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<CustodyAssignmentDetailDto>.Ok(result, "Custody assigned successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustodyAssignmentDetailDto>>> Update(
        Guid id, CustodyAssignmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustodyAssignmentDetailDto>.Ok(
            await service.UpdateAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustodyAssignmentDetailDto>>> Acknowledge(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustodyAssignmentDetailDto>.Ok(
            await service.AcknowledgeAsync(id, cancellationToken), "Receipt acknowledged."));

    [HttpPost("{id:guid}/dispute")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustodyAssignmentDetailDto>>> Dispute(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustodyAssignmentDetailDto>.Ok(
            await service.DisputeAsync(id, cancellationToken), "Custody marked as disputed."));

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustodyAssignmentDetailDto>>> Close(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustodyAssignmentDetailDto>.Ok(
            await service.CloseAsync(id, cancellationToken), "Custody assignment closed."));
}
