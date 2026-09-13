namespace AssetsManagement.Domain;

public sealed class MaintenancePlan : AuditableEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ScopeKind { get; set; } = "Asset type";
    public Guid? ScopeId { get; set; }
    public string TriggerKind { get; set; } = "Calendar interval";
    public string? TaskList { get; set; }
    public int? IntervalMonths { get; set; }
}

public sealed class MaintenanceRequest : AuditableEntity
{
    public string RequestNumber { get; set; } = "";
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public Guid? RaisedById { get; set; }
    public DateTime RaisedOnUtc { get; set; }
    public string FaultDescription { get; set; } = "";
    public string Urgency { get; set; } = "Normal";
    public bool AssetUsable { get; set; } = true;
    public Guid? PhotographId { get; set; }
    public string? LocationAtReport { get; set; }
    public string State { get; set; } = MaintenanceRequestStates.Submitted;
    public Guid? TriagedById { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
}

public sealed class WorkOrder : AuditableEntity
{
    public string WorkOrderNumber { get; set; } = "";
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string WorkKind { get; set; } = "Corrective";
    public string Source { get; set; } = "Manual";
    public Guid? SourceRequestId { get; set; }
    public string Priority { get; set; } = "Normal";
    public DateTime? ScheduledStart { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid? ProviderId { get; set; }
    public bool UnderWarranty { get; set; }
    public bool AssetOutOfService { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public decimal DowntimeHours { get; set; }
    public string? WorkDone { get; set; }
    public decimal TotalCost { get; set; }
    public string State { get; set; } = WorkOrderStates.Scheduled;
    public ICollection<WorkOrderLine> Lines { get; set; } = [];
}

public sealed class WorkOrderLine : Entity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;
    public int LineNumber { get; set; }
    public string LineKind { get; set; } = "Labor";
    public string Description { get; set; } = "";
    public Guid? TechnicianId { get; set; }
    public decimal? Hours { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal LineCost { get; set; }
    public bool Chargeable { get; set; } = true;
}

public sealed class AssetInspection : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string InspectionKind { get; set; } = "Condition";
    public DateTime InspectedOn { get; set; }
    public Guid InspectorId { get; set; }
    public string Condition { get; set; } = "Good";
    public bool LocationConfirmed { get; set; }
    public bool CustodianConfirmed { get; set; }
    public string? Findings { get; set; }
    public Guid? PhotographId { get; set; }
    public string ActionRequired { get; set; } = "None";
    public Guid? WorkOrderId { get; set; }
    public DateTime? NextDue { get; set; }
}
