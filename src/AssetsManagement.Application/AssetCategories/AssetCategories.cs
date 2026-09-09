using FluentValidation;

namespace AssetsManagement.Application;

public sealed class AssetCategoryListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? ParentCategory { get; init; }
}

public sealed class AssetCategoryRequest
{
    public string Name { get; init; } = "";
    public string? Code { get; init; }
    public bool? Active { get; init; }
    public bool? MoreInformation { get; init; }
    public string? AlternateName { get; init; }
    public string? ParentCategory { get; init; }
    public string? AccountCode { get; init; }
}

public sealed record AssetCategoryListItemDto(Guid Id, string Name, string? Code, bool Active);

public sealed record AssetCategoryTypeItemDto(string AssetType, string Code, bool Active);

public sealed record AssetCategoryDetailDto(
    Guid Id,
    string Name,
    string? Code,
    bool Active,
    string? AlternateName,
    string? ParentCategory,
    string? AccountCode,
    string Lifecycle,
    IReadOnlyCollection<AssetCategoryTypeItemDto> AssetTypes);

public sealed record AssetCategoryHistoryDto(
    DateTime When,
    string Change,
    string? Field,
    string? OldValue,
    string? NewValue,
    string By,
    string Source);

public interface IAssetCategoryService
{
    Task<PagedResult<AssetCategoryListItemDto>> ListAsync(AssetCategoryListQuery query, CancellationToken cancellationToken);
    Task<AssetCategoryDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetCategoryDetailDto> CreateAsync(AssetCategoryRequest request, CancellationToken cancellationToken);
    Task<AssetCategoryDetailDto> UpdateAsync(Guid id, AssetCategoryRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetCategoryDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetCategoryHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetCategoryListQueryValidator : AbstractValidator<AssetCategoryListQuery>
{
    public AssetCategoryListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class AssetCategoryRequestValidator : AbstractValidator<AssetCategoryRequest>
{
    public AssetCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));
        RuleFor(x => x.Active).NotNull().WithMessage("Active is required.");
        RuleFor(x => x.MoreInformation).NotNull().WithMessage("More information is required.");
        When(x => x.MoreInformation == true, () =>
        {
            RuleFor(x => x.AlternateName).MaximumLength(200);
            RuleFor(x => x.ParentCategory).MaximumLength(200);
            RuleFor(x => x.AccountCode).MaximumLength(100);
            RuleFor(x => x.ParentCategory)
                .Must((request, parent) => !string.Equals(parent?.Trim(), request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                .When(x => !string.IsNullOrWhiteSpace(x.ParentCategory) && !string.IsNullOrWhiteSpace(x.Name))
                .WithMessage("A category cannot be its own parent.");
        });
    }
}
