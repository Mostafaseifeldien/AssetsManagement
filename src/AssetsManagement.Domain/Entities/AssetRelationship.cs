namespace AssetsManagement.Domain;

public sealed class AssetRelationship : AuditableEntity
{
    public Guid SourceAssetId { get; set; }
    public Asset SourceAsset { get; set; } = null!;
    public Guid TargetAssetId { get; set; }
    public Asset TargetAsset { get; set; } = null!;
    public string RelationshipType { get; set; } = "";
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public decimal? Quantity { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = RelationshipStatuses.Active;
}
