using FluentValidation;

namespace AssetsManagement.Application;

public sealed class RfidTagListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public string? TagType { get; init; }
    public string? EncodingStandard { get; init; }
    public string? Status { get; init; }
    public string? Asset { get; init; }
}

public sealed class RfidTagRequest
{
    public string TagIdentifier { get; init; } = "";
    public string TagType { get; init; } = "";
    public string? EncodingStandard { get; init; }
    public string? Status { get; init; }
    public string? Asset { get; init; }
    public bool? MoreInformation { get; init; }
}

public sealed record RfidTagListItemDto(
    Guid Id,
    string TagIdentifier,
    string TagType,
    string? EncodingStandard,
    string Status,
    bool MoreInformation);

public sealed record RfidTagDetailDto(
    Guid Id,
    string TagIdentifier,
    string TagType,
    string? EncodingStandard,
    string Status,
    bool MoreInformation,
    string? Asset,
    DateTime? EncodedAt,
    string? EncodedBy,
    string? ReplacedBy,
    DateTime? RetiredAt,
    string Lifecycle);

public sealed record RfidTagHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public sealed record RfidWaitingAssetDto(
    Guid Id,
    string Name,
    string AssetNumber,
    string? AssetType);

public interface IRfidTagService
{
    Task<PagedResult<RfidTagListItemDto>> ListAsync(RfidTagListQuery query, CancellationToken cancellationToken);
    Task<RfidTagDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<RfidTagDetailDto> CreateAsync(RfidTagRequest request, CancellationToken cancellationToken);
    Task<RfidTagDetailDto> UpdateAsync(Guid id, RfidTagRequest request, CancellationToken cancellationToken);
    Task<RfidTagDetailDto> AssignAsync(Guid id, Guid assetId, CancellationToken cancellationToken);
    Task<RfidTagDetailDto> UnassignAsync(Guid id, CancellationToken cancellationToken);
    Task<RfidTagDetailDto> ReplaceAsync(Guid id, Guid replacementId, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<RfidTagDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RfidTagHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string tagIdentifier, Guid? excludingId, CancellationToken cancellationToken);
    Task<PagedResult<RfidWaitingAssetDto>> ListAssetsWaitingAsync(
        RfidTagListQuery query, CancellationToken cancellationToken);
}

public sealed class RfidTagListQueryValidator : AbstractValidator<RfidTagListQuery>
{
    public RfidTagListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class RfidTagRequestValidator : AbstractValidator<RfidTagRequest>
{
    public static readonly string[] TagTypes = ["Passive UHF", "Passive HF", "Active", "Hybrid"];
    public static readonly string[] EncodingStandards = ["GS1 SGTIN", "Custom", "Other"];
    public static readonly string[] Statuses = ["Unassigned", "Assigned", "Damaged", "Replaced", "Retired"];

    public RfidTagRequestValidator()
    {
        RuleFor(x => x.TagIdentifier).NotEmpty().MaximumLength(128)
            .Matches("^[A-Za-z0-9:_-]+$")
            .WithMessage("Tag identifier must be an EPC or TID code.");
        RuleFor(x => x.TagType).NotEmpty().Must(x => TagTypes.Contains(x))
            .WithMessage("Tag type must be Passive UHF, Passive HF, Active or Hybrid.");
        RuleFor(x => x.EncodingStandard).Must(x => EncodingStandards.Contains(x!))
            .When(x => !string.IsNullOrWhiteSpace(x.EncodingStandard))
            .WithMessage("Encoding standard must be GS1 SGTIN, Custom or Other.");
        RuleFor(x => x.Status).NotEmpty().Must(x => Statuses.Contains(x!))
            .WithMessage("Status must be Unassigned, Assigned, Damaged, Replaced or Retired.");
        RuleFor(x => x.MoreInformation).NotNull().WithMessage("More information is required.");
        RuleFor(x => x.Asset).NotEmpty()
            .When(x => string.Equals(x.Status, "Assigned", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Asset is required when the tag is assigned.");
        RuleFor(x => x.Asset).Empty()
            .When(x => string.Equals(x.Status, "Unassigned", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("An unassigned tag cannot be linked to an asset.");
        RuleFor(x => x.Asset).MaximumLength(200);
    }
}
