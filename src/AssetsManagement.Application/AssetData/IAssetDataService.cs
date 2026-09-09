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

    Task<PagedResult<TypeAttributeDto>> ListTypeAttributesAsync(ListQuery query, CancellationToken cancellationToken);
    Task<TypeAttributeDto> GetTypeAttributeAsync(Guid id, CancellationToken cancellationToken);
    Task<TypeAttributeDto> CreateTypeAttributeAsync(TypeAttributeRequest request, CancellationToken cancellationToken);
    Task<TypeAttributeDto> CreateTypeAttributeDefinitionAsync(Guid assetTypeId, CreateTypeAttributeDefinitionRequest request, CancellationToken cancellationToken);
    Task<TypeAttributeDto> UpdateTypeAttributeAsync(Guid id, TypeAttributeRequest request, CancellationToken cancellationToken);
    Task DeactivateTypeAttributeAsync(Guid id, CancellationToken cancellationToken);
    Task<TypeAttributeDto> RestoreTypeAttributeAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AssetLookupDto>> AssetLookupAsync(string? search, CancellationToken cancellationToken);
}
