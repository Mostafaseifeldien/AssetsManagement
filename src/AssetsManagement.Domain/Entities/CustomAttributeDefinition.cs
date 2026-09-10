namespace AssetsManagement.Domain;

public sealed class CustomAttributeDefinition : CodedMasterEntity
{
    public CustomAttributeDataType DataType { get; set; }
    public string? ListValuesJson { get; set; }
    public string? Unit { get; set; }
    public string? HelpText { get; set; }
    public string? AlternateHelpText { get; set; }
    public ICollection<AssetTypeAttribute> AssetTypes { get; set; } = [];
}
