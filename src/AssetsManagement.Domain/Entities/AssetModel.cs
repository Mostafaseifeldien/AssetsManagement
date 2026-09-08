namespace AssetsManagement.Domain;

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
