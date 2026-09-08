namespace AssetsManagement.Application;

public sealed class MasterDataRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? AlternateName { get; init; }
    public string? Description { get; init; }
    public string? NumberingFormat { get; init; }
    public Guid? AssetCategoryId { get; init; }
    public bool RequiresSerialNumber { get; init; }
    public bool RequiresRfidTag { get; init; }
    public bool RequiresBarcode { get; init; }
    public Guid? DefaultStatusId { get; init; }
    public string? PermittedStatusTransitions { get; init; }
    public string? CustomAttributeSchema { get; init; }
    public string? DefaultDepreciationMethod { get; init; }
    public int? DefaultUsefulLifeMonths { get; init; }
    public Guid? ParentId { get; init; }
    public string? AccountCode { get; init; }
    public Guid? ManufacturerId { get; init; }
    public Guid? AssetTypeId { get; init; }
    public string? ModelNumber { get; init; }
    public string? Specifications { get; init; }
    public int? ExpectedUsefulLifeMonths { get; init; }
    public string? Documentation { get; init; }
    public string? Country { get; init; }
    public string? SupportContact { get; init; }
    public string? Website { get; init; }
    public string? SupplierKind { get; init; }
    public string? TaxRegistration { get; init; }
    public string? ContactPerson { get; init; }
    public string? Telephone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? PaymentTerms { get; init; }
    public string? Rating { get; init; }
    public string? ExternalIdentifier { get; init; }
    public string? StatusCategory { get; init; }
    public string? Color { get; init; }
    public bool IsOperational { get; init; }
    public bool IsTerminal { get; init; }
    public bool BlocksMovement { get; init; }
    public int DisplayOrder { get; init; }
    public string? DataType { get; init; }
    public IReadOnlyCollection<string>? ListValues { get; init; }
    public string? Unit { get; init; }
    public string? HelpText { get; init; }
    public string? AlternateHelpText { get; init; }
}
