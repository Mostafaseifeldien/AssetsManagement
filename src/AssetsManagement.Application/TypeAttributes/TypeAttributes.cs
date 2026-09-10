using FluentValidation;

namespace AssetsManagement.Application;

public sealed class TypeAttributeListQuery
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
    public string? Class { get; init; }
    public string? Requirement { get; init; }
    public bool? ShowInList { get; init; }
}

public sealed class TypeAttributeFieldRequest
{
    public string? AssetType { get; init; }
    public int? DisplayOrder { get; init; }
    public string Label { get; init; } = "";
    public string? AlternateName { get; init; }
    public string Code { get; init; } = "";
    public string? DataType { get; init; }
    public string? Unit { get; init; }
    public IReadOnlyCollection<string>? PossibleValues { get; init; }
    public string? Class { get; init; }
    public bool? ShowInList { get; init; }
    public string? HelpText { get; init; }
    public string Requirement { get; init; } = "Optional";
}

public sealed record TypeAttributeListItemDto(
    Guid Id,
    Guid AssetTypeId,
    string AssetType,
    string Label,
    string? AlternateName,
    string Code,
    string DataType,
    IReadOnlyCollection<string>? PossibleValues,
    string? Unit,
    string Class,
    bool ShowInList,
    string? HelpText,
    bool Active);

public sealed record TypeAttributeDetailDto(
    Guid Id,
    Guid AssetTypeId,
    string AssetType,
    Guid DefinitionId,
    string Label,
    string? AlternateName,
    string Code,
    bool CodeIsLocked,
    string DataType,
    IReadOnlyCollection<string>? PossibleValues,
    string? Unit,
    string Class,
    int DisplayOrder,
    bool ShowInList,
    string? HelpText,
    bool Active,
    int AssetCount);

public sealed record TypeAttributeGroupDto(
    Guid AssetTypeId,
    string AssetType,
    int AssetCount,
    int ExtraFields,
    IReadOnlyCollection<TypeAttributeListItemDto> Attributes);

public sealed record TypeAttributeTypeOptionDto(
    Guid Id,
    string Name,
    int AssetCount);

public interface IAssetTypeAttributeService
{
    Task<PagedResult<TypeAttributeListItemDto>> ListAsync(
        TypeAttributeListQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TypeAttributeGroupDto>> ListGroupedAsync(
        TypeAttributeListQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TypeAttributeTypeOptionDto>> ListTypesAsync(CancellationToken cancellationToken);
    Task<TypeAttributeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<TypeAttributeDetailDto> CreateFieldAsync(
        Guid? assetTypeId, TypeAttributeFieldRequest request, CancellationToken cancellationToken);
    Task<TypeAttributeDetailDto> UpdateFieldAsync(
        Guid id, TypeAttributeFieldRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<TypeAttributeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string code, string? assetType, Guid? excludingId, CancellationToken cancellationToken);
}

public sealed class TypeAttributeListQueryValidator : AbstractValidator<TypeAttributeListQuery>
{
    public TypeAttributeListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class TypeAttributeFieldRequestValidator : AbstractValidator<TypeAttributeFieldRequest>
{
    public static readonly string[] DataTypes =
        ["Text", "Number", "Money", "Date", "Yes or no", "List"];
    public static readonly string[] Classes = ["Optional", "Recommended", "Required"];

    public TypeAttributeFieldRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200)
            .WithMessage("A label is needed — it is what people read on the form.");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[a-z][a-z0-9_]*$")
            .WithMessage("A code is lower case letters, digits and underscores, starting with a letter.");
        RuleFor(x => x.DataType).NotEmpty().Must(IsDataType)
            .WithMessage("Data type must be Text, Number, Money, Date, Yes or no or List.");
        RuleFor(x => x.PossibleValues)
            .Must(x => x is not null && x.Count(v => !string.IsNullOrWhiteSpace(v)) >= 2)
            .When(x => CustomAttributeDefinitionRequestValidator.IsList(x.DataType))
            .WithMessage("A list needs at least two values to choose between.");
        RuleFor(x => x).Must(x => Classes.Contains(ResolveClass(x)))
            .WithName(nameof(TypeAttributeFieldRequest.Class))
            .WithMessage("Class must be Optional, Recommended or Required.");
        RuleFor(x => x.AlternateName).MaximumLength(200);
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.HelpText).MaximumLength(1000);
    }

    public static string ResolveClass(TypeAttributeFieldRequest request) =>
        !string.IsNullOrWhiteSpace(request.Class) ? request.Class.Trim() : request.Requirement.Trim();

    public static bool IsDataType(string? value)
    {
        var formatted = CustomAttributeDefinitionRequestValidator.FormatDataType(value);
        return formatted is not null && DataTypes.Contains(formatted);
    }
}
