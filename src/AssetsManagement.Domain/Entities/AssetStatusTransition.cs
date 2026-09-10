namespace AssetsManagement.Domain;

public sealed class AssetStatusTransition : Entity
{
    public Guid FromStatusId { get; set; }
    public AssetStatus FromStatus { get; set; } = null!;
    public Guid ToStatusId { get; set; }
    public AssetStatus ToStatus { get; set; } = null!;
}
