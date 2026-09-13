using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/exit-authorizations")]
public sealed class ExitAuthorizationsController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ExitAuthorizationListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ExitAuthorizationListItemDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<ExitAuthorizationListItemDto>>.Ok(
            await service.ListExitsAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExitAuthorizationDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ExitAuthorizationDetailDto>.Ok(
            await service.GetExitAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ExitAuthorizationDetailDto>>> Create(
        ExitAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateExitAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<ExitAuthorizationDetailDto>.Ok(result, "Exit authorization requested successfully."));
    }

    [HttpPost("{id:guid}/decide")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ExitAuthorizationDetailDto>>> Decide(
        Guid id, ExitDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ExitAuthorizationDetailDto>.Ok(
            await service.DecideExitAsync(id, request, cancellationToken), "Decision recorded."));

    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ExitAuthorizationDetailDto>>> Return(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ExitAuthorizationDetailDto>.Ok(
            await service.ReturnExitAsync(id, cancellationToken), "Asset marked as returned."));
}
