namespace AssetsManagement.Domain;

public sealed class AssetCategory : CodedMasterEntity
{
    public Guid? ParentId { get; set; }
    public AssetCategory? Parent { get; set; }
    public string? AccountCode { get; set; }
    public bool MoreInformation { get; set; }
    public ICollection<AssetCategory> Children { get; set; } = [];
}
