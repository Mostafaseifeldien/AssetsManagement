namespace AssetsManagement.Domain;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public abstract class CodedMasterEntity : AuditableEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AlternateName { get; set; }
    public string? Description { get; set; }
}

public sealed class AssetType : CodedMasterEntity
{
    public Guid? AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }
    public bool RequiresSerialNumber { get; set; }
    public bool RequiresRfidTag { get; set; }
    public bool RequiresBarcode { get; set; }
    public Guid? DefaultStatusId { get; set; }
    public AssetStatus? DefaultStatus { get; set; }
    public string? PermittedStatusTransitions { get; set; }
    public string? CustomAttributeSchema { get; set; }
    public string? DefaultDepreciationMethod { get; set; }
    public int? DefaultUsefulLifeMonths { get; set; }
    public string? NumberingFormat { get; set; }
    public ICollection<AssetTypeAttribute> Attributes { get; set; } = [];
}

public sealed class AssetCategory : CodedMasterEntity
{
    public Guid? ParentId { get; set; }
    public AssetCategory? Parent { get; set; }
    public string? AccountCode { get; set; }
    public ICollection<AssetCategory> Children { get; set; } = [];
}

public sealed class Manufacturer : CodedMasterEntity
{
    public string? Country { get; set; }
    public string? SupportContact { get; set; }
    public string? Website { get; set; }
}

public sealed class Supplier : CodedMasterEntity
{
    public string SupplierKind { get; set; } = "General";
    public string? TaxRegistration { get; set; }
    public string? ContactPerson { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Rating { get; set; }
    public string? ExternalIdentifier { get; set; }
}

public sealed class AssetStatus : CodedMasterEntity
{
    public string StatusCategory { get; set; } = "Unknown";
    public string Color { get; set; } = "#6b7280";
    public bool IsOperational { get; set; }
    public bool IsTerminal { get; set; }
    public bool BlocksMovement { get; set; }
    public int DisplayOrder { get; set; }
    public ICollection<AssetStatusTransition> AllowedFrom { get; set; } = [];
    public ICollection<AssetStatusTransition> AllowedTo { get; set; } = [];
}

public sealed class AssetStatusTransition : Entity
{
    public Guid FromStatusId { get; set; }
    public AssetStatus FromStatus { get; set; } = null!;
    public Guid ToStatusId { get; set; }
    public AssetStatus ToStatus { get; set; } = null!;
}

public sealed class AssetModel : CodedMasterEntity
{
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; } = null!;
    public Guid? AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }
    public string ModelNumber { get; set; } = "";
    public string? Specifications { get; set; }
    public int? ExpectedUsefulLifeMonths { get; set; }
    public string? Documentation { get; set; }
}

public sealed class Asset : CodedMasterEntity
{
    public string AssetNumber { get; set; } = "";
    public Guid AssetTypeId { get; set; }
    public AssetType AssetType { get; set; } = null!;
    public Guid? AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }
    public Guid? AssetModelId { get; set; }
    public AssetModel? AssetModel { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid AssetStatusId { get; set; }
    public AssetStatus AssetStatus { get; set; } = null!;
    public ICollection<AssetImage> Images { get; set; } = [];
}

public enum CustomAttributeDataType
{
    Text, Number, Money, Date, YesNo, List, Reference
}

public enum AttributeRequirement
{
    Optional, Recommended, Required
}

public sealed class CustomAttributeDefinition : CodedMasterEntity
{
    public CustomAttributeDataType DataType { get; set; }
    public string? ListValuesJson { get; set; }
    public string? Unit { get; set; }
    public string? HelpText { get; set; }
    public string? AlternateHelpText { get; set; }
    public ICollection<AssetTypeAttribute> AssetTypes { get; set; } = [];
}

public sealed class AssetTypeAttribute : AuditableEntity
{
    public Guid AssetTypeId { get; set; }
    public AssetType AssetType { get; set; } = null!;
    public Guid CustomAttributeDefinitionId { get; set; }
    public CustomAttributeDefinition CustomAttributeDefinition { get; set; } = null!;
    public AttributeRequirement Requirement { get; set; }
    public int DisplayOrder { get; set; }
    public bool ShowInList { get; set; }
    public bool HasRecordedValues { get; set; }
}

public enum IdentifierStatus
{
    Unassigned, Assigned, Retired, Damaged
}

public sealed class RfidTag : AuditableEntity
{
    public string TagIdentifier { get; set; } = "";
    public string TagType { get; set; } = "";
    public string EncodingStandard { get; set; } = "";
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public IdentifierStatus Status { get; set; } = IdentifierStatus.Unassigned;
    public DateTime? EncodedAtUtc { get; set; }
    public string? EncodedBy { get; set; }
    public Guid? ReplacedById { get; set; }
    public RfidTag? ReplacedBy { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
}

public sealed class Barcode : AuditableEntity
{
    public string Value { get; set; } = "";
    public string Symbology { get; set; } = "Code128";
    public string SubjectType { get; set; } = "Asset";
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public IdentifierStatus Status { get; set; } = IdentifierStatus.Unassigned;
    public DateTime? PrintedAtUtc { get; set; }
    public Guid? ReplacedById { get; set; }
    public Barcode? ReplacedBy { get; set; }
}

public sealed class AssetImage : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string StoredFileName { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string? Caption { get; set; }
    public string? Purpose { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public string? CapturedBy { get; set; }
}

public sealed class ChangeHistoryEntry : Entity
{
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public DateTime WhenUtc { get; set; }
    public string Change { get; set; } = "";
    public string? Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string By { get; set; } = "";
    public string Source { get; set; } = "Screen";
}

public sealed class DomainRuleException(string message) : Exception(message);

public static class AssetDataRules
{
    public static void EnsureCategoryParentIsValid(Guid categoryId, Guid? parentId) =>
        _ = parentId == categoryId
            ? throw new DomainRuleException("A category cannot be its own parent.")
            : true;

    public static void EnsureAttributeCodeCanChange(bool hasRecordedValues, string oldCode, string newCode)
    {
        if (hasRecordedValues && !string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("The attribute code is immutable after a value has been recorded.");
    }

    public static void EnsureIdentifierCanBeAssigned(IdentifierStatus status, Guid? assetId)
    {
        if (status != IdentifierStatus.Unassigned || assetId is not null)
            throw new DomainRuleException("Only an unassigned identifier in stock can be assigned.");
    }
}
