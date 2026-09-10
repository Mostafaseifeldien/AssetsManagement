using AssetsManagement.Domain;
using FluentValidation;

namespace AssetsManagement.Application;

public class AssetListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public Guid? AssetTypeId { get; init; }
    public Guid? AssetCategoryId { get; init; }
    public Guid? AssetStatusId { get; init; }
    public Guid? AssetModelId { get; init; }
    public Guid? ManufacturerId { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? CurrentCustodianId { get; init; }
    public string? OwningDepartment { get; init; }
    public string? Criticality { get; init; }
}

public sealed class MissingAssetQuery : AssetListQuery
{
    public int MissingAfterMinutes { get; init; } = 15;
    public string? Silence { get; init; }
}

public sealed class AssetRequest
{
    public string Name { get; init; } = "";
    public string? AlternateName { get; init; }
    public Guid? AssetTypeId { get; init; }
    public Guid? AssetStatusId { get; init; }
    public string? AssetNumber { get; init; }
    public Guid? AssetCategoryId { get; init; }
    public Guid? AssetModelId { get; init; }
    public Guid? ManufacturerId { get; init; }
    public Guid? SupplierId { get; init; }
    public string? SerialNumber { get; init; }
    public string? OwningOrganization { get; init; }
    public string? OwningDepartment { get; init; }
    public string? CostCenter { get; init; }
    public Guid? CurrentCustodianId { get; init; }
    public string? CustodianType { get; init; }
    public string? CurrentLocation { get; init; }
    public string? LocationSource { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public decimal? PurchaseValue { get; init; }
    public string? PurchaseReference { get; init; }
    public DateTime? WarrantyExpiry { get; init; }
    public string? DepreciationMethod { get; init; }
    public int? UsefulLifeMonths { get; init; }
    public decimal? ResidualValue { get; init; }
    public string? Criticality { get; init; }
    public Guid? ParentAssetId { get; init; }
    public DateTime? CommissionedDate { get; init; }
    public DateTime? DisposalDate { get; init; }
    public string? DisposalReason { get; init; }
    public Dictionary<string, string?>? CustomAttributes { get; init; }
    public bool? Active { get; init; }
}

public sealed class AssetStatusChangeRequest
{
    public Guid AssetStatusId { get; init; }
    public string? Reason { get; init; }
}

public sealed class AssetLocationRequest
{
    public string CurrentLocation { get; init; } = "";
    public string LocationSource { get; init; } = "Manual";
    public string? LastSeenReader { get; init; }
}

public sealed record AssetListItemDto(
    Guid Id,
    string Name,
    string AssetNumber,
    Guid AssetTypeId,
    Guid AssetStatusId,
    Guid? AssetCategoryId,
    Guid? CurrentCustodianId,
    string? OwningDepartment,
    string? CurrentLocation,
    string? Criticality,
    decimal? PurchaseValue,
    bool Active);

public sealed record AssetDetailDto(
    Guid Id,
    string Name,
    string? AlternateName,
    string AssetNumber,
    Guid AssetTypeId,
    Guid AssetStatusId,
    Guid? AssetCategoryId,
    Guid? AssetModelId,
    Guid? ManufacturerId,
    Guid? SupplierId,
    string? SerialNumber,
    string? OwningOrganization,
    string? OwningDepartment,
    string? CostCenter,
    Guid? CurrentCustodianId,
    string? CustodianType,
    string? CurrentLocation,
    DateTime? LocationUpdatedAt,
    string? LocationSource,
    DateTime? LastSeenAt,
    string? LastSeenReader,
    DateTime? PurchaseDate,
    decimal? PurchaseValue,
    string? PurchaseReference,
    DateTime? WarrantyExpiry,
    string? DepreciationMethod,
    int? UsefulLifeMonths,
    decimal? ResidualValue,
    string? Criticality,
    Guid? ParentAssetId,
    DateTime? CommissionedDate,
    DateTime? DisposalDate,
    string? DisposalReason,
    IReadOnlyDictionary<string, string?> CustomAttributes,
    Guid? RfidTagId,
    Guid? BarcodeId,
    Guid? PrimaryImageId,
    bool Active,
    string Lifecycle);

public sealed record MissingAssetListItemDto(
    Guid Id,
    string Name,
    string AssetNumber,
    Guid AssetTypeId,
    Guid? CurrentCustodianId,
    string? OwningDepartment,
    string? CurrentLocation,
    DateTime? LastSeenAt,
    int SilentForMinutes,
    string? Criticality,
    decimal? PurchaseValue);

public sealed record MissingAssetsResult(
    int NotReporting,
    int SilentOverADay,
    int HighCriticality,
    int TotalAssets,
    int PercentOfAssets,
    PagedResult<MissingAssetListItemDto> Page);

public sealed record AssetHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IAssetService
{
    Task<PagedResult<AssetListItemDto>> ListAsync(AssetListQuery query, CancellationToken cancellationToken);
    Task<MissingAssetsResult> ListMissingAsync(MissingAssetQuery query, CancellationToken cancellationToken);
    Task<AssetDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetDetailDto> CreateAsync(AssetRequest request, CancellationToken cancellationToken);
    Task<AssetDetailDto> UpdateAsync(Guid id, AssetRequest request, CancellationToken cancellationToken);
    Task<AssetDetailDto> ChangeStatusAsync(Guid id, AssetStatusChangeRequest request, CancellationToken cancellationToken);
    Task<AssetDetailDto> UpdateLocationAsync(Guid id, AssetLocationRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetLookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetListQueryValidator : AbstractValidator<AssetListQuery>
{
    public AssetListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
        RuleFor(x => x.Criticality).Must(x => x is null || Criticalities.Contains(x))
            .WithMessage("Criticality must be High, Medium or Low.");
    }

    public static readonly string[] Criticalities = ["High", "Medium", "Low"];
}

public sealed class MissingAssetQueryValidator : AbstractValidator<MissingAssetQuery>
{
    public MissingAssetQueryValidator()
    {
        Include(new AssetListQueryValidator());
        RuleFor(x => x.MissingAfterMinutes).InclusiveBetween(1, 525600);
        RuleFor(x => x.Silence).Must(x => x is null or "Over a day" or "Over an hour" or "Under an hour")
            .WithMessage("Silence must be Over a day, Over an hour or Under an hour.");
    }
}

public sealed class AssetRequestValidator : AbstractValidator<AssetRequest>
{
    public AssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetTypeId).NotEmpty().WithMessage("Asset type id is required.");
        RuleFor(x => x.AlternateName).MaximumLength(200);
        RuleFor(x => x.AssetNumber).MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.OwningOrganization).MaximumLength(200);
        RuleFor(x => x.OwningDepartment).MaximumLength(200);
        RuleFor(x => x.CostCenter).MaximumLength(100);
        RuleFor(x => x.CustodianType).Must(x => x is null || CustodyTypes.All.Contains(x))
            .WithMessage("Custodian type must be Employee, Contractor, Department or Team.");
        RuleFor(x => x.CurrentLocation).MaximumLength(200);
        RuleFor(x => x.LocationSource).Must(x => x is null || LocationSources.All.Contains(x))
            .WithMessage("Location source must be Manual, Inventory, Reader or Operation.");
        RuleFor(x => x.PurchaseReference).MaximumLength(100);
        RuleFor(x => x.DepreciationMethod).MaximumLength(100);
        RuleFor(x => x.UsefulLifeMonths).InclusiveBetween(1, 1200).When(x => x.UsefulLifeMonths.HasValue);
        RuleFor(x => x.PurchaseValue).GreaterThanOrEqualTo(0).When(x => x.PurchaseValue.HasValue);
        RuleFor(x => x.ResidualValue).GreaterThanOrEqualTo(0).When(x => x.ResidualValue.HasValue);
        RuleFor(x => x.Criticality).Must(x => x is null || AssetListQueryValidator.Criticalities.Contains(x))
            .WithMessage("Criticality must be High, Medium or Low.");
        RuleFor(x => x.DisposalReason).MaximumLength(200);
        RuleFor(x => x.ParentAssetId).NotEqual(x => x.AssetTypeId)
            .When(x => x.ParentAssetId.HasValue);
    }
}

public sealed class AssetStatusChangeRequestValidator : AbstractValidator<AssetStatusChangeRequest>
{
    public AssetStatusChangeRequestValidator() =>
        RuleFor(x => x.AssetStatusId).NotEmpty();
}

public sealed class AssetLocationRequestValidator : AbstractValidator<AssetLocationRequest>
{
    public AssetLocationRequestValidator()
    {
        RuleFor(x => x.CurrentLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LocationSource).NotEmpty().Must(x => LocationSources.All.Contains(x))
            .WithMessage("Location source must be Manual, Inventory, Reader or Operation.");
        RuleFor(x => x.LastSeenReader).MaximumLength(100);
    }
}
