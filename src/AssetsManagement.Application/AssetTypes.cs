using FluentValidation;

namespace AssetsManagement.Application;

public sealed class AssetTypeListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? AssetCategory { get; init; }
}

public sealed class AssetTypeRequest
{
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string? AssetCategory { get; init; }
    public string? RequiresSerialNumber { get; init; }
    public string? DefaultStatus { get; init; }
    public string? Active { get; init; }
    public string? MoreInformation { get; init; }
    public string? AlternateName { get; init; }
    public string? RequiresRfidTag { get; init; }
    public string? RequiresBarcode { get; init; }
    public string? PermittedStatusTransitions { get; init; }
    public string? CustomAttributeSchema { get; init; }
    public string? DefaultDepreciationMethod { get; init; }
    public int? DefaultUsefulLife { get; init; }
    public string? NumberingScheme { get; init; }
}

public sealed record AssetTypeListItemDto(
    Guid Id,
    string Name,
    string Code,
    string? AssetCategory,
    bool RequiresSerialNumber,
    string? DefaultStatus,
    bool Active);

public sealed record AssetTypeDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? AssetCategory,
    string RequiresSerialNumber,
    string? DefaultStatus,
    string Active,
    string? AlternateName,
    string RequiresRfidTag,
    string RequiresBarcode,
    string? PermittedStatusTransitions,
    string? CustomAttributeSchema,
    string? DefaultDepreciationMethod,
    int? DefaultUsefulLife,
    string? NumberingScheme,
    string Lifecycle);

public sealed record AssetTypeHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IAssetTypeService
{
    Task<PagedResult<AssetTypeListItemDto>> ListAsync(AssetTypeListQuery query, CancellationToken cancellationToken);
    Task<AssetTypeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetTypeDetailDto> CreateAsync(AssetTypeRequest request, CancellationToken cancellationToken);
    Task<AssetTypeDetailDto> UpdateAsync(Guid id, AssetTypeRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetTypeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetTypeHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetTypeListQueryValidator : AbstractValidator<AssetTypeListQuery>
{
    public AssetTypeListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class AssetTypeRequestValidator : AbstractValidator<AssetTypeRequest>
{
    public AssetTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$");
        RuleFor(x => x.AssetCategory).MaximumLength(200);
        RuleFor(x => x.RequiresSerialNumber).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Requires serial number must be Yes or No.");
        RuleFor(x => x.DefaultStatus).MaximumLength(200);
        RuleFor(x => x.Active).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Active must be Yes or No.");
        RuleFor(x => x.MoreInformation).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("More information must be Yes or No.");
        When(x => YesNoParser.TryParse(x.MoreInformation) == true, () =>
        {
            RuleFor(x => x.AlternateName).MaximumLength(200);
            RuleFor(x => x.RequiresRfidTag).Must(YesNoParser.IsYesNo)
                .When(x => !string.IsNullOrWhiteSpace(x.RequiresRfidTag))
                .WithMessage("Requires RFID tag must be Yes or No.");
            RuleFor(x => x.RequiresBarcode).Must(YesNoParser.IsYesNo)
                .When(x => !string.IsNullOrWhiteSpace(x.RequiresBarcode))
                .WithMessage("Requires barcode must be Yes or No.");
            RuleFor(x => x.PermittedStatusTransitions).MaximumLength(4000);
            RuleFor(x => x.CustomAttributeSchema).MaximumLength(4000);
            RuleFor(x => x.DefaultDepreciationMethod).MaximumLength(100);
            RuleFor(x => x.DefaultUsefulLife).InclusiveBetween(1, 1200)
                .When(x => x.DefaultUsefulLife.HasValue);
            RuleFor(x => x.NumberingScheme).MaximumLength(50);
        });
    }
}
