using FluentValidation;

namespace AssetsManagement.Application;

public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);

public sealed record ApiResponse<T>(bool Success, string Message, T? Data, IReadOnlyCollection<ApiError> Errors)
{
    public static ApiResponse<T> Ok(T data, string message = "Records retrieved successfully.") =>
        new(true, message, data, []);
}

public sealed record ApiError(string? Field, string Message, string? Code = null);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed class ListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? IsActive { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? Status { get; init; }
    public Guid? AssetCategoryId { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? ManufacturerId { get; init; }
    public Guid? AssetTypeId { get; init; }
    public string? SupplierKind { get; init; }
    public string? Rating { get; init; }
    public string? StatusCategory { get; init; }
    public string? DataType { get; init; }
}

public enum AssetDataResource
{
    AssetTypes,
    AssetCategories,
    AssetModels,
    Manufacturers,
    Suppliers,
    AssetStatuses,
    CustomAttributeDefinitions
}

public sealed class MasterDataRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? AlternateName { get; init; }
    public string? Description { get; init; }
    public string? NumberingFormat { get; init; }
    public Guid? AssetCategoryId { get; init; }
    public bool RequiresSerialNumber { get; init; }
    public bool RequiresRfidTag { get; init; }
    public bool RequiresBarcode { get; init; }
    public Guid? DefaultStatusId { get; init; }
    public string? PermittedStatusTransitions { get; init; }
    public string? CustomAttributeSchema { get; init; }
    public string? DefaultDepreciationMethod { get; init; }
    public int? DefaultUsefulLifeMonths { get; init; }
    public Guid? ParentId { get; init; }
    public string? AccountCode { get; init; }
    public Guid? ManufacturerId { get; init; }
    public Guid? AssetTypeId { get; init; }
    public string? ModelNumber { get; init; }
    public string? Specifications { get; init; }
    public int? ExpectedUsefulLifeMonths { get; init; }
    public string? Documentation { get; init; }
    public string? Country { get; init; }
    public string? SupportContact { get; init; }
    public string? Website { get; init; }
    public string? SupplierKind { get; init; }
    public string? TaxRegistration { get; init; }
    public string? ContactPerson { get; init; }
    public string? Telephone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? PaymentTerms { get; init; }
    public string? Rating { get; init; }
    public string? ExternalIdentifier { get; init; }
    public string? StatusCategory { get; init; }
    public string? Color { get; init; }
    public bool IsOperational { get; init; }
    public bool IsTerminal { get; init; }
    public bool BlocksMovement { get; init; }
    public int DisplayOrder { get; init; }
    public string? DataType { get; init; }
    public IReadOnlyCollection<string>? ListValues { get; init; }
    public string? Unit { get; init; }
    public string? HelpText { get; init; }
    public string? AlternateHelpText { get; init; }
}

public sealed record AssetDataDto(
    Guid Id,
    string Code,
    string Name,
    string? AlternateName,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? RowVersion,
    IReadOnlyDictionary<string, object?> Details);

public sealed class TypeAttributeRequest
{
    public Guid AssetTypeId { get; init; }
    public Guid CustomAttributeDefinitionId { get; init; }
    public string Requirement { get; init; } = "Optional";
    public int DisplayOrder { get; init; } = 1;
    public bool ShowInList { get; init; }
}

public sealed class CreateTypeAttributeDefinitionRequest
{
    public string Code { get; init; } = "";
    public string Label { get; init; } = "";
    public string? AlternateName { get; init; }
    public string DataType { get; init; } = "Text";
    public IReadOnlyCollection<string>? ListValues { get; init; }
    public string? Unit { get; init; }
    public string Requirement { get; init; } = "Optional";
    public int DisplayOrder { get; init; } = 1;
    public bool ShowInList { get; init; }
    public string? HelpText { get; init; }
    public string? AlternateHelpText { get; init; }
}

public sealed record TypeAttributeDto(
    Guid Id, Guid AssetTypeId, string AssetType, Guid DefinitionId, string Code, string Label,
    string DataType, string Requirement, int DisplayOrder, bool ShowInList, bool IsActive);

public sealed record StatusTransitionRequest(IReadOnlyCollection<Guid> AllowedToStatusIds);

public sealed class RfidTagRequest
{
    public string TagIdentifier { get; init; } = "";
    public string TagType { get; init; } = "";
    public string EncodingStandard { get; init; } = "";
}

public sealed class BarcodeRequest
{
    public string Value { get; init; } = "";
    public string Symbology { get; init; } = "Code128";
    public string SubjectType { get; init; } = "Asset";
}

public sealed record IdentifierDto(
    Guid Id, string Value, string Kind, string Status, Guid? AssetId, string? AssetName,
    DateTime? AssignedAtUtc, bool IsActive);

public sealed record AssetLookupDto(Guid Id, string AssetNumber, string Name);
public sealed record LookupDto(Guid Id, string Code, string Name);

public sealed class ImageUpload
{
    public required Stream Content { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public string? Caption { get; init; }
    public string? Purpose { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed record AssetImageDto(
    Guid Id, Guid AssetId, string AssetName, string OriginalFileName, string ContentType,
    long SizeBytes, string? Caption, string? Purpose, bool IsPrimary, bool IsLocked, string ContentUrl,
    DateTime CreatedAtUtc);

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResult(string AccessToken, string TokenType, DateTime ExpiresAt, string Username, IReadOnlyCollection<string> Roles);

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

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

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    string UserName { get; }
    string DisplayName { get; }
}

public sealed class ListQueryValidator : AbstractValidator<ListQuery>
{
    public ListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class MasterDataRequestValidator : AbstractValidator<MasterDataRequest>
{
    public MasterDataRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AlternateName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Website).Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultUsefulLifeMonths).GreaterThan(0).When(x => x.DefaultUsefulLifeMonths.HasValue);
        RuleFor(x => x.ExpectedUsefulLifeMonths).GreaterThan(0).When(x => x.ExpectedUsefulLifeMonths.HasValue);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.Color));
        RuleFor(x => x.SupplierKind).Must(x => x is "Vendor" or "Service provider" or "Both")
            .When(x => !string.IsNullOrWhiteSpace(x.SupplierKind));
        RuleFor(x => x.Rating).Must(x => x is "Preferred" or "Approved" or "Under review" or "Blocked")
            .When(x => !string.IsNullOrWhiteSpace(x.Rating));
        RuleFor(x => x.StatusCategory).Must(x => x is "Working" or "Damaged" or "In Maintenance" or
                "Missing" or "In Transit" or "Disposed" or "Unknown")
            .When(x => !string.IsNullOrWhiteSpace(x.StatusCategory));
        RuleFor(x => x.DataType).Must(x => x is "Text" or "Number" or "Money" or "Date" or
                "YesNo" or "Yes or no" or "Yes, No" or "List" or "Reference")
            .When(x => !string.IsNullOrWhiteSpace(x.DataType));
        RuleFor(x => x.ListValues).NotEmpty()
            .When(x => string.Equals(x.DataType, "List", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class TypeAttributeRequestValidator : AbstractValidator<TypeAttributeRequest>
{
    public TypeAttributeRequestValidator()
    {
        RuleFor(x => x.AssetTypeId).NotEmpty();
        RuleFor(x => x.CustomAttributeDefinitionId).NotEmpty();
        RuleFor(x => x.Requirement).Must(x => x is "Optional" or "Recommended" or "Required");
        RuleFor(x => x.DisplayOrder).GreaterThan(0);
    }
}

public sealed class CreateTypeAttributeDefinitionRequestValidator : AbstractValidator<CreateTypeAttributeDefinitionRequest>
{
    public CreateTypeAttributeDefinitionRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[a-z][a-z0-9_]*$");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataType).Must(x => x is "Text" or "Number" or "Money" or "Date" or
            "YesNo" or "Yes or no" or "Yes, No" or "List" or "Reference");
        RuleFor(x => x.ListValues).NotEmpty().When(x => x.DataType == "List");
        RuleFor(x => x.Requirement).Must(x => x is "Optional" or "Recommended" or "Required");
        RuleFor(x => x.DisplayOrder).GreaterThan(0);
    }
}

public sealed class RfidTagRequestValidator : AbstractValidator<RfidTagRequest>
{
    public RfidTagRequestValidator()
    {
        RuleFor(x => x.TagIdentifier).NotEmpty().MaximumLength(128)
            .Matches("^[A-Za-z0-9:_-]+$");
        RuleFor(x => x.TagType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EncodingStandard).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TagType).Must(x => x is "Passive UHF" or "Passive HF" or "Active" or "Hybrid");
        RuleFor(x => x.EncodingStandard).Must(x => x is "GS1 SGTIN" or "Custom" or "Other");
    }
}

public sealed class BarcodeRequestValidator : AbstractValidator<BarcodeRequest>
{
    public BarcodeRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(128)
            .Matches("^[A-Za-z0-9._-]+$");
        RuleFor(x => x.Symbology).Must(x => x is "Code128" or "Code39" or "QR" or "DataMatrix" or "EAN")
            .WithMessage("Supported symbologies are Code128, Code39, QR, DataMatrix and EAN.");
        RuleFor(x => x.SubjectType).Equal("Asset");
    }
}
