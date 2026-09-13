namespace AssetsManagement.Domain;

public sealed class AssetPosition : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string FloorPlan { get; set; } = "";
    public string? Room { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public string PositionSource { get; set; } = "Manual";
    public decimal? Confidence { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string? RecordedBy { get; set; }
    public string? DerivedFromReader { get; set; }
    public bool Valid { get; set; } = true;
    public bool IsCurrent { get; set; }
}

public sealed class ExitAuthorization : AuditableEntity
{
    public string Number { get; set; } = "";
    public string Status { get; set; } = ExitAuthorizationStates.PendingOwner;
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public Guid RequestedById { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? Destination { get; set; }
    public DateTime? ExpectedReturn { get; set; }
    public Guid? CarrierId { get; set; }
    public string? GateScope { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public Guid? OwnerApproverId { get; set; }
    public string? OwnerDecision { get; set; }
    public DateTime? OwnerDecidedAtUtc { get; set; }
    public Guid? ManagerApproverId { get; set; }
    public string? ManagerDecision { get; set; }
    public DateTime? ManagerDecidedAtUtc { get; set; }
    public Guid? SecurityApproverId { get; set; }
    public string? SecurityDecision { get; set; }
    public DateTime? SecurityDecidedAtUtc { get; set; }
}
