namespace AssetsManagement.Domain;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
