using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/custody-transfers")]
public sealed class CustodyTransfersController(ICustodyService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustodyTransferListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustodyTransferListItemDto>>>> List(
        [FromQuery] CustodyListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<CustodyTransferListItemDto>>.Ok(
            await service.ListTransfersAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustodyTransferDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustodyTransferDetailDto>.Ok(
            await service.GetTransferAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustodyTransferDetailDto>>> Transfer(
        CustodyTransferRequest request, CancellationToken cancellationToken)
    {
        var result = await service.TransferAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<CustodyTransferDetailDto>.Ok(result, "Custody transferred successfully."));
    }
}
