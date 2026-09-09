using FluentValidation;

namespace AssetsManagement.Application;

public sealed class CustomAttributeDefinitionListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? AssetType { get; init; }
    public Guid? AssetTypeId { get; init; }
    public string? DataType { get; init; }
    public string? EffectiveClass { get; init; }
}

public sealed class CustomAttributeDefinitionRequest
{
    public string AssetType { get; init; } = "";
    public string Code { get; init; } = "";
    public string Label { get; init; } = "";
    public string? DataType { get; init; }
    public string? EffectiveClass { get; init; }
    public int? DisplayOrder { get; init; }
    public string? ShowInList { get; init; }
    public string? Active { get; init; }
    public string? MoreInformation { get; init; }
    public string? AlternateName { get; init; }
    public IReadOnlyCollection<string>? ListValues { get; init; }
    public string? Unit { get; init; }
    public string? HelpText { get; init; }
    public string? AlternateHelpText { get; init; }
}

public sealed record CustomAttributeDefinitionListItemDto(
    Guid Id,
    string AssetType,
    string Code,
    string Label,
    string DataType,
    string EffectiveClass,
    int DisplayOrder);

public sealed record CustomAttributeDefinitionDetailDto(
    Guid Id,
    string AssetType,
    string Code,
    string Label,
    string DataType,
    string EffectiveClass,
    int DisplayOrder,
    string ShowInList,
    string Active,
    string? AlternateName,
    IReadOnlyCollection<string>? ListValues,
    string? Unit,
    string? HelpText,
    string? AlternateHelpText,
    string Lifecycle);

public sealed record CustomAttributeDefinitionHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface ICustomAttributeDefinitionService
{
    Task<PagedResult<CustomAttributeDefinitionListItemDto>> ListAsync(
        CustomAttributeDefinitionListQuery query, CancellationToken cancellationToken);
    Task<CustomAttributeDefinitionDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CustomAttributeDefinitionDetailDto> CreateAsync(
        CustomAttributeDefinitionRequest request, CancellationToken cancellationToken);
    Task<CustomAttributeDefinitionDetailDto> UpdateAsync(
        Guid id, CustomAttributeDefinitionRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<CustomAttributeDefinitionDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CustomAttributeDefinitionHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string code, string? assetType, Guid? excludingId, CancellationToken cancellationToken);
}

public sealed class CustomAttributeDefinitionListQueryValidator : AbstractValidator<CustomAttributeDefinitionListQuery>
{
    public CustomAttributeDefinitionListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class CustomAttributeDefinitionRequestValidator : AbstractValidator<CustomAttributeDefinitionRequest>
{
    public static readonly string[] DataTypes =
        ["Text", "Number", "Money", "Date", "Yes or no", "List", "Reference"];
    public static readonly string[] Classes = ["Required", "Recommended", "Optional"];

    public CustomAttributeDefinitionRequestValidator()
    {
        RuleFor(x => x.AssetType).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[a-z][a-z0-9_]*$")
            .WithMessage("Code must be a lower_case identifier.");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataType).NotEmpty().Must(IsDataType)
            .WithMessage("Data type must be Text, Number, Money, Date, Yes or no, List or Reference.");
        RuleFor(x => x.EffectiveClass).NotEmpty().Must(x => Classes.Contains(x!))
            .WithMessage("Effective class must be Required, Recommended or Optional.");
        RuleFor(x => x.DisplayOrder).GreaterThan(0).When(x => x.DisplayOrder.HasValue);
        RuleFor(x => x.ShowInList).Must(YesNoParser.IsYesNo)
            .When(x => !string.IsNullOrWhiteSpace(x.ShowInList))
            .WithMessage("Show in list must be Yes or No.");
        RuleFor(x => x.Active).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Active must be Yes or No.");
        RuleFor(x => x.MoreInformation).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("More information must be Yes or No.");
        RuleFor(x => x.AlternateName).MaximumLength(200);
        RuleFor(x => x.ListValues).NotEmpty()
            .When(x => IsList(x.DataType))
            .WithMessage("List values are required when the data type is List.");
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.HelpText).MaximumLength(1000);
        RuleFor(x => x.AlternateHelpText).MaximumLength(1000);
    }

    public static bool IsList(string? dataType) =>
        string.Equals(FormatDataType(dataType), "List", StringComparison.OrdinalIgnoreCase);

    public static bool IsDataType(string? value) => FormatDataType(value) is not null &&
        DataTypes.Contains(FormatDataType(value)!);

    public static string? FormatDataType(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        return trimmed.ToLowerInvariant() switch
        {
            "yes or no" or "yesno" or "yes, no" or "yes/no" or "yes_or_no" or "yes-or-no" or "yes" => "Yes or no",
            "text" => "Text",
            "number" => "Number",
            "money" => "Money",
            "date" => "Date",
            "list" => "List",
            "reference" => "Reference",
            _ => trimmed
        };
    }
}
