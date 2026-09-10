namespace AssetsManagement.Domain;

public sealed class CustodyAssignment : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public Guid CustodianId { get; set; }
    public string CustodianType { get; set; } = CustodyTypes.Employee;
    public DateTime AssignedFromUtc { get; set; }
    public DateTime? AssignedToUtc { get; set; }
    public string AssignedBy { get; set; } = "";
    public string? AssignmentReason { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string Status { get; set; } = CustodyStatuses.Active;
    public string? HandoverDocument { get; set; }
    public Guid? BatchId { get; set; }
    public CustodyTransferBatch? Batch { get; set; }
}
