using FluentValidation;

namespace AssetsManagement.Application;

public sealed class AssetImageListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? Asset { get; init; }
    public string? Purpose { get; init; }
    public bool? IsPrimary { get; init; }
}

public sealed class AssetImageCreateRequest
{
    public string? Asset { get; init; }
    public string? IsPrimary { get; init; }
    public string? Purpose { get; init; }
    public string? MoreInformation { get; init; }
    public string? Caption { get; init; }
}

public sealed class AssetImageRequest
{
    public string Asset { get; init; } = "";
    public string? IsPrimary { get; init; }
    public string? Purpose { get; init; }
    public string? MoreInformation { get; init; }
    public string? Caption { get; init; }
}

public sealed class ImageUpload
{
    public required Stream Content { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
}

public sealed record AssetImageListItemDto(
    Guid Id,
    string Asset,
    string File,
    bool IsPrimary,
    string? Purpose);

public sealed record AssetImageDetailDto(
    Guid Id,
    string Asset,
    string File,
    string IsPrimary,
    string? Purpose,
    string? Caption,
    DateTime? CapturedAt,
    string? CapturedBy,
    bool IsLocked,
    string ContentUrl,
    string Lifecycle);

public sealed record AssetImageHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IAssetImageService
{
    Task<PagedResult<AssetImageListItemDto>> ListAsync(AssetImageListQuery query, CancellationToken cancellationToken);
    Task<AssetImageDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetImageDetailDto> CreateAsync(
        AssetImageCreateRequest request, ImageUpload upload, CancellationToken cancellationToken);
    Task<AssetImageDetailDto> UpdateAsync(Guid id, AssetImageRequest request, CancellationToken cancellationToken);
    Task<AssetImageDetailDto> SetPrimaryAsync(Guid id, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetImageDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetImageHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType, string FileName)> OpenAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetImageListQueryValidator : AbstractValidator<AssetImageListQuery>
{
    public AssetImageListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class AssetImageCreateRequestValidator : AbstractValidator<AssetImageCreateRequest>
{
    public AssetImageCreateRequestValidator()
    {
        RuleFor(x => x.Asset).NotEmpty();
        RuleFor(x => x.IsPrimary).Must(YesNoParser.IsYesNo)
            .When(x => !string.IsNullOrWhiteSpace(x.IsPrimary))
            .WithMessage("Is primary must be Yes or No.");
        RuleFor(x => x.Purpose).Must(AssetImageRequestValidator.IsPurpose)
            .When(x => !string.IsNullOrWhiteSpace(x.Purpose))
            .WithMessage("Purpose must be Identification, Condition record, Damage evidence or Nameplate.");
        RuleFor(x => x.MoreInformation).Must(YesNoParser.IsYesNo)
            .When(x => !string.IsNullOrWhiteSpace(x.MoreInformation))
            .WithMessage("More information must be Yes or No.");
        RuleFor(x => x.Caption).MaximumLength(500);
    }
}

public sealed class AssetImageRequestValidator : AbstractValidator<AssetImageRequest>
{
    public static readonly string[] Purposes =
        ["Identification", "Condition record", "Damage evidence", "Nameplate"];

    public AssetImageRequestValidator()
    {
        RuleFor(x => x.Asset).NotEmpty();
        RuleFor(x => x.IsPrimary).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Is primary must be Yes or No.");
        RuleFor(x => x.Purpose).Must(IsPurpose)
            .When(x => !string.IsNullOrWhiteSpace(x.Purpose))
            .WithMessage("Purpose must be Identification, Condition record, Damage evidence or Nameplate.");
        RuleFor(x => x.MoreInformation).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("More information must be Yes or No.");
        RuleFor(x => x.Caption).MaximumLength(500);
    }

    public static bool IsPurpose(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Purposes.Contains(value);
}
