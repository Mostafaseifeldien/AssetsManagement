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
    public int DisplayOrder { get; init; } = 1;
    public bool ShowInList { get; init; }
    public string? HelpText { get; init; }
    public string? AlternateHelpText { get; init; }
}

public sealed record TypeAttributeDto(
    Guid Id, Guid AssetTypeId, string AssetType, Guid DefinitionId, string Code, string Label,
    string DataType, string Requirement, int DisplayOrder, bool ShowInList, bool IsActive);

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
        RuleFor(x => x.DataType).Must(x => x is "Text" or "Number" or "Money" or "Date" or
            "YesNo" or "Yes or no" or "Yes, No" or "List" or "Reference");
        RuleFor(x => x.ListValues).NotEmpty().When(x => x.DataType == "List");
        RuleFor(x => x.Requirement).Must(x => x is "Optional" or "Recommended" or "Required");
        RuleFor(x => x.DisplayOrder).GreaterThan(0);
    }
}
