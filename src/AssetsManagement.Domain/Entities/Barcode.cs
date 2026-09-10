namespace AssetsManagement.Domain;

public sealed class Barcode : AuditableEntity
{
    public string Value { get; set; } = "";
    public string Symbology { get; set; } = "Code128";
    public string SubjectType { get; set; } = "Asset";
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public IdentifierStatus Status { get; set; } = IdentifierStatus.Unassigned;
    public DateTime? PrintedAtUtc { get; set; }
    public Guid? ReplacedById { get; set; }
    public Barcode? ReplacedBy { get; set; }
    public bool MoreInformation { get; set; }
}
