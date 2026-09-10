namespace AssetsManagement.Domain;

public sealed class CustodyTransferBatch : AuditableEntity
{
    public string BatchNumber { get; set; } = "";
    public Guid CustodianId { get; set; }
    public string CustodianType { get; set; } = CustodyTypes.Employee;
    public string Reason { get; set; } = "";
    public DateTime AssignedFromUtc { get; set; }
    public bool RequireAcknowledgement { get; set; } = true;
    public string AssignedBy { get; set; } = "";
    public int AssetCount { get; set; }
    public ICollection<CustodyAssignment> Assignments { get; set; } = [];
}
