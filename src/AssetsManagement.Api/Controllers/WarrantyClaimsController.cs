using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/warranty-claims")]
public sealed class WarrantyClaimsController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<WarrantyClaimListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<WarrantyClaimListItemDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<WarrantyClaimListItemDto>>.Ok(
            await service.ListClaimsAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarrantyClaimDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WarrantyClaimDetailDto>.Ok(
            await service.GetClaimAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarrantyClaimDetailDto>>> Create(
        WarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateClaimAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<WarrantyClaimDetailDto>.Ok(result, "Claim raised successfully."));
    }

    [HttpPost("{id:guid}/settle")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarrantyClaimDetailDto>>> Settle(
        Guid id, WarrantyClaimDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WarrantyClaimDetailDto>.Ok(
            await service.SettleClaimAsync(id, request, cancellationToken), "Claim settled successfully."));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarrantyClaimDetailDto>>> Reject(
        Guid id, WarrantyClaimDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WarrantyClaimDetailDto>.Ok(
            await service.RejectClaimAsync(id, request, cancellationToken), "Claim rejected."));
}
