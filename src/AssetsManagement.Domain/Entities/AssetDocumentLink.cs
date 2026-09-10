namespace AssetsManagement.Domain;

public sealed class AssetDocumentLink : Entity
{
    public Guid AssetDocumentId { get; set; }
    public AssetDocument AssetDocument { get; set; } = null!;
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public decimal? AllocatedAmount { get; set; }
}
