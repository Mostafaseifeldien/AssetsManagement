namespace AssetsManagement.Domain;

public sealed class AssetDocument : AuditableEntity
{
    public string DocumentKind { get; set; } = "";
    public string Title { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? IssuedBy { get; set; }
    public string? ReferenceNumber { get; set; }
    public bool Confidential { get; set; }
    public int Version { get; set; } = 1;
    public string State { get; set; } = DocumentStates.Current;
    public decimal? Amount { get; set; }
    public Guid? SupersededById { get; set; }
    public AssetDocument? SupersededBy { get; set; }
    public string UploadedBy { get; set; } = "";
    public ICollection<AssetDocumentLink> Links { get; set; } = [];
}
