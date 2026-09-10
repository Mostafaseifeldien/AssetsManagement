namespace AssetsManagement.Domain;

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
    public bool MoreInformation { get; set; }
    public ICollection<AssetTypeAttribute> Attributes { get; set; } = [];
}
