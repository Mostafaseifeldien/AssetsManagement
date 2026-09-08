namespace AssetsManagement.Domain;

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
