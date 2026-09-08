using FluentValidation;

namespace AssetsManagement.Application;

public sealed class AssetModelListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? Manufacturer { get; init; }
    public string? AssetType { get; init; }
}

public sealed class AssetModelRequest
{
    public string Name { get; init; } = "";
    public string? Manufacturer { get; init; }
    public string ModelNumber { get; init; } = "";
    public string? Active { get; init; }
    public string? MoreInformation { get; init; }
    public string? AlternateName { get; init; }
    public string? AssetType { get; init; }
    public string? Specifications { get; init; }
    public int? ExpectedUsefulLife { get; init; }
    public string? Documentation { get; init; }
}

public sealed record AssetModelListItemDto(
    Guid Id,
    string Name,
    string Manufacturer,
    string ModelNumber,
    bool Active);

public sealed record AssetModelDetailDto(
    Guid Id,
    string Name,
    string Manufacturer,
    string ModelNumber,
    string Active,
    string? AlternateName,
    string? AssetType,
    string? Specifications,
    int? ExpectedUsefulLife,
    string? Documentation,
    string Lifecycle);

public sealed record AssetModelHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IAssetModelService
{
    Task<PagedResult<AssetModelListItemDto>> ListAsync(AssetModelListQuery query, CancellationToken cancellationToken);
    Task<AssetModelDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetModelDetailDto> CreateAsync(AssetModelRequest request, CancellationToken cancellationToken);
    Task<AssetModelDetailDto> UpdateAsync(Guid id, AssetModelRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetModelDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetModelHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetModelListQueryValidator : AbstractValidator<AssetModelListQuery>
{
    public AssetModelListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class AssetModelRequestValidator : AbstractValidator<AssetModelRequest>
{
    public AssetModelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(200)
            .WithMessage("Manufacturer is required.");
        RuleFor(x => x.ModelNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Active).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Active must be Yes or No.");
        RuleFor(x => x.MoreInformation).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("More information must be Yes or No.");
        When(x => YesNoParser.TryParse(x.MoreInformation) == true, () =>
        {
            RuleFor(x => x.AlternateName).MaximumLength(200);
            RuleFor(x => x.AssetType).MaximumLength(200);
            RuleFor(x => x.Specifications).MaximumLength(4000);
            RuleFor(x => x.ExpectedUsefulLife).InclusiveBetween(1, 1200)
                .When(x => x.ExpectedUsefulLife.HasValue);
            RuleFor(x => x.Documentation).MaximumLength(4000);
        });
    }
}
