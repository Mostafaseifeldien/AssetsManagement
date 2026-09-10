namespace AssetsManagement.Domain;

public sealed class AssetImage : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string StoredFileName { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string? Caption { get; set; }
    public string? Purpose { get; set; }
    public bool IsPrimary { get; set; }
    public bool MoreInformation { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public string? CapturedBy { get; set; }
}
