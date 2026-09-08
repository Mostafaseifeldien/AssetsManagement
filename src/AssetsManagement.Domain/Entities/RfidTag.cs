namespace AssetsManagement.Domain;

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
