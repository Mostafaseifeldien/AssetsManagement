using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[Route("api/asset-statuses")]
public sealed class AssetStatusesController(IAssetDataService service)
    : MasterDataController(service, AssetDataResource.AssetStatuses)
{
    [HttpGet("{id:guid}/allowed-transitions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> AllowedTransitions(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await Service.GetAllowedStatusTransitionsAsync(id, cancellationToken)));

    [HttpPut("{id:guid}/allowed-transitions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LookupDto>>>> SetAllowedTransitions(
        Guid id, StatusTransitionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<LookupDto>>.Ok(
            await Service.SetAllowedStatusTransitionsAsync(id, request, cancellationToken),
            "Allowed transitions updated successfully."));
}
