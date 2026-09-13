using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[ApiController]
[Route("api/asset-expenses")]
public sealed class AssetExpensesController(IAssetOperationsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetExpenseListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetExpenseListItemDto>>>> List(
        [FromQuery] AssetChildListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetExpenseListItemDto>>.Ok(
            await service.ListExpensesAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetExpenseDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetExpenseDetailDto>.Ok(
            await service.GetExpenseAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<OperationHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<OperationHistoryDto>>.Ok(
            await service.GetExpenseHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetExpenseDetailDto>>> Create(
        AssetExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateExpenseAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetExpenseDetailDto>.Ok(result, "Expense recorded successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetExpenseDetailDto>>> Update(
        Guid id, AssetExpenseRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetExpenseDetailDto>.Ok(
            await service.UpdateExpenseAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/reverse")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetExpenseDetailDto>>> Reverse(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetExpenseDetailDto>.Ok(
            await service.ReverseExpenseAsync(id, cancellationToken), "Expense reversed successfully."));
}
