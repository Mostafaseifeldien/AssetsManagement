using FluentValidation;

namespace AssetsManagement.Application;

public sealed class AssetStatusListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? StatusCategory { get; init; }
    public bool? IsOperational { get; init; }
    public bool? IsTerminal { get; init; }
}

public sealed class AssetStatusRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? StatusCategory { get; init; }
    public string? Color { get; init; }
    public string? IsOperational { get; init; }
    public string? IsTerminal { get; init; }
    public string? BlocksMovement { get; init; }
    public string? Active { get; init; }
    public string? MoreInformation { get; init; }
    public string? AlternateName { get; init; }
}

public sealed record AssetStatusListItemDto(
    Guid Id,
    string Code,
    string Name,
    string StatusCategory,
    string Color,
    bool IsOperational,
    bool IsTerminal);

public sealed record AssetStatusDetailDto(
    Guid Id,
    string Code,
    string Name,
    string StatusCategory,
    string Color,
    string IsOperational,
    string IsTerminal,
    string BlocksMovement,
    string Active,
    string? AlternateName,
    int SortOrder,
    string Lifecycle,
    IReadOnlyCollection<LookupDto> AllowedTransitions);

public sealed record AssetStatusHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IAssetStatusService
{
    Task<PagedResult<AssetStatusListItemDto>> ListAsync(AssetStatusListQuery query, CancellationToken cancellationToken);
    Task<AssetStatusDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetStatusDetailDto> CreateAsync(AssetStatusRequest request, CancellationToken cancellationToken);
    Task<AssetStatusDetailDto> UpdateAsync(Guid id, AssetStatusRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetStatusDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> GetAllowedTransitionsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> SetAllowedTransitionsAsync(
        Guid id, StatusTransitionRequest request, CancellationToken cancellationToken);
}

public sealed class AssetStatusListQueryValidator : AbstractValidator<AssetStatusListQuery>
{
    public AssetStatusListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class AssetStatusRequestValidator : AbstractValidator<AssetStatusRequest>
{
    public static readonly string[] Categories =
        ["Working", "Damaged", "In Maintenance", "Missing", "In Transit", "Disposed", "Unknown"];

    public AssetStatusRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StatusCategory).NotEmpty().Must(IsCategory)
            .WithMessage("Status category must be Working, Damaged, In Maintenance, Missing, In Transit, Disposed or Unknown.");
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a hex value such as #16a34a.");
        RuleFor(x => x.IsOperational).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Is operational must be Yes or No.");
        RuleFor(x => x.IsTerminal).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Is terminal must be Yes or No.");
        RuleFor(x => x.BlocksMovement).Must(YesNoParser.IsYesNo)
            .When(x => !string.IsNullOrWhiteSpace(x.BlocksMovement))
            .WithMessage("Blocks movement must be Yes or No.");
        RuleFor(x => x.Active).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Active must be Yes or No.");
        RuleFor(x => x.MoreInformation).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("More information must be Yes or No.");
        RuleFor(x => x.AlternateName).MaximumLength(200);
    }

    public static bool IsCategory(string? value)
    {
        var normalized = NormalizeCategory(value);
        return normalized is not null && Categories.Contains(normalized);
    }

    public static string? NormalizeCategory(string? value)
    {
        var trimmed = value?.Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

public sealed class StatusTransitionRequestValidator : AbstractValidator<StatusTransitionRequest>
{
    public StatusTransitionRequestValidator()
    {
        RuleFor(x => x.AllowedToStatusIds).NotNull();
    }
}
