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
    Task<IReadOnlyCollection<LookupDto>> GetAllowedStatusTransitionsAsync(Guid statusId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> SetAllowedStatusTransitionsAsync(Guid statusId, StatusTransitionRequest request, CancellationToken cancellationToken);

    Task<PagedResult<TypeAttributeDto>> ListTypeAttributesAsync(ListQuery query, CancellationToken cancellationToken);
    Task<TypeAttributeDto> GetTypeAttributeAsync(Guid id, CancellationToken cancellationToken);
    Task<TypeAttributeDto> CreateTypeAttributeAsync(TypeAttributeRequest request, CancellationToken cancellationToken);
    Task<TypeAttributeDto> CreateTypeAttributeDefinitionAsync(Guid assetTypeId, CreateTypeAttributeDefinitionRequest request, CancellationToken cancellationToken);
    Task<TypeAttributeDto> UpdateTypeAttributeAsync(Guid id, TypeAttributeRequest request, CancellationToken cancellationToken);
    Task DeactivateTypeAttributeAsync(Guid id, CancellationToken cancellationToken);
    Task<TypeAttributeDto> RestoreTypeAttributeAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<IdentifierDto>> ListRfidTagsAsync(ListQuery query, CancellationToken cancellationToken);
    Task<IdentifierDto> GetRfidTagAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentifierDto> CreateRfidTagAsync(RfidTagRequest request, CancellationToken cancellationToken);
    Task<IdentifierDto> UpdateRfidTagAsync(Guid id, RfidTagRequest request, CancellationToken cancellationToken);
    Task<IdentifierDto> AssignRfidTagAsync(Guid id, Guid assetId, CancellationToken cancellationToken);
    Task<IdentifierDto> UnassignRfidTagAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentifierDto> ReplaceRfidTagAsync(Guid id, Guid replacementId, CancellationToken cancellationToken);
    Task DeactivateRfidTagAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentifierDto> RestoreRfidTagAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> RfidExistsAsync(string value, CancellationToken cancellationToken);

    Task<PagedResult<IdentifierDto>> ListBarcodesAsync(ListQuery query, CancellationToken cancellationToken);
    Task<IdentifierDto> GetBarcodeAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentifierDto> CreateBarcodeAsync(BarcodeRequest request, CancellationToken cancellationToken);
    Task<IdentifierDto> UpdateBarcodeAsync(Guid id, BarcodeRequest request, CancellationToken cancellationToken);
    Task<IdentifierDto> GenerateBarcodeAsync(string symbology, CancellationToken cancellationToken);
    Task<IdentifierDto> AssignBarcodeAsync(Guid id, Guid assetId, CancellationToken cancellationToken);
    Task<IdentifierDto> UnassignBarcodeAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentifierDto> ReplaceBarcodeAsync(Guid id, Guid replacementId, CancellationToken cancellationToken);
    Task DeactivateBarcodeAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentifierDto> RestoreBarcodeAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> BarcodeExistsAsync(string value, CancellationToken cancellationToken);

    Task<PagedResult<AssetImageDto>> ListImagesAsync(ListQuery query, CancellationToken cancellationToken);
    Task<AssetImageDto> GetImageAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetImageDto> UploadImageAsync(Guid assetId, ImageUpload upload, CancellationToken cancellationToken);
    Task<AssetImageDto> UpdateImageAsync(Guid id, string? caption, string? purpose, CancellationToken cancellationToken);
    Task<AssetImageDto> SetPrimaryImageAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteImageAsync(Guid id, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType, string FileName)> OpenImageAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetLookupDto>> AssetLookupAsync(string? search, CancellationToken cancellationToken);
}
