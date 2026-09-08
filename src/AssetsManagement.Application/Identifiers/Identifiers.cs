using FluentValidation;

namespace AssetsManagement.Application;

public sealed class RfidTagRequest
{
    public string TagIdentifier { get; init; } = "";
    public string TagType { get; init; } = "";
    public string EncodingStandard { get; init; } = "";
}

public sealed class BarcodeRequest
{
    public string Value { get; init; } = "";
    public string Symbology { get; init; } = "Code128";
    public string SubjectType { get; init; } = "Asset";
}

public sealed record IdentifierDto(
    Guid Id, string Value, string Kind, string Status, Guid? AssetId, string? AssetName,
    DateTime? AssignedAtUtc, bool IsActive);

public sealed class RfidTagRequestValidator : AbstractValidator<RfidTagRequest>
{
    public RfidTagRequestValidator()
    {
        RuleFor(x => x.TagIdentifier).NotEmpty().MaximumLength(128)
            .Matches("^[A-Za-z0-9:_-]+$");
        RuleFor(x => x.TagType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EncodingStandard).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TagType).Must(x => x is "Passive UHF" or "Passive HF" or "Active" or "Hybrid");
        RuleFor(x => x.EncodingStandard).Must(x => x is "GS1 SGTIN" or "Custom" or "Other");
    }
}

public sealed class BarcodeRequestValidator : AbstractValidator<BarcodeRequest>
{
    public BarcodeRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(128)
            .Matches("^[A-Za-z0-9._-]+$");
        RuleFor(x => x.Symbology).Must(x => x is "Code128" or "Code39" or "QR" or "DataMatrix" or "EAN")
            .WithMessage("Supported symbologies are Code128, Code39, QR, DataMatrix and EAN.");
        RuleFor(x => x.SubjectType).Equal("Asset");
    }
}
