using FluentValidation;

namespace AssetsManagement.Application;

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
    public int? DisplayOrder { get; init; }
    public bool ShowInList { get; init; }
    public string? HelpText { get; init; }
    public string? AlternateHelpText { get; init; }
}

public sealed record TypeAttributeDto(
    Guid Id, Guid AssetTypeId, string AssetType, Guid DefinitionId, string Code, string Label,
    string DataType, string Requirement, int DisplayOrder, bool ShowInList, bool IsActive,
    IReadOnlyCollection<string>? ListValues, string? Unit, string? HelpText);

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
        RuleFor(x => x.DataType).Must(x => CustomAttributeDefinitionRequestValidator.IsDataType(x))
            .WithMessage("Data type must be Text, Number, Money, Date, Yes or no, List or Reference.");
        RuleFor(x => x.ListValues).NotEmpty()
            .When(x => CustomAttributeDefinitionRequestValidator.IsList(x.DataType))
            .WithMessage("List values are required when the data type is List.");
        RuleFor(x => x.Requirement).Must(x => x is "Optional" or "Recommended" or "Required");
        RuleFor(x => x.DisplayOrder).GreaterThan(0).When(x => x.DisplayOrder.HasValue);
    }
}
