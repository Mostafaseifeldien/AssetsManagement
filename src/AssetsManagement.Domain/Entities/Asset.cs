namespace AssetsManagement.Domain;

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
