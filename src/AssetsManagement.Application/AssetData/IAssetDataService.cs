namespace AssetsManagement.Application;

public interface IAssetDataService
{
    Task<PagedResult<AssetDataDto>> ListAsync(AssetDataResource resource, ListQuery query, CancellationToken cancellationToken);
    Task<AssetDataDto> GetAsync(AssetDataResource resource, Guid id, CancellationToken cancellationToken);
    Task<AssetDataDto> CreateAsync(AssetDataResource resource, MasterDataRequest request, CancellationToken cancellationToken);
    Task<AssetDataDto> UpdateAsync(AssetDataResource resource, Guid id, MasterDataRequest request, string? rowVersion, CancellationToken cancellationToken);
    Task DeactivateAsync(AssetDataResource resource, Guid id, CancellationToken cancellationToken);
    Task<AssetDataDto> RestoreAsync(AssetDataResource resource, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(AssetDataResource resource, string? search, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(AssetDataResource resource, string code, Guid? excludingId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AssetLookupDto>> AssetLookupAsync(string? search, CancellationToken cancellationToken);
}
