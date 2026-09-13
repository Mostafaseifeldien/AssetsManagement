namespace AssetsManagement.Domain;

public sealed class AssetExpense : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string ExpenseKind { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public DateTime ExpenseDate { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool Capitalized { get; set; }
    public string? CostCenter { get; set; }
    public Guid? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public Guid? DocumentId { get; set; }
    public string? Notes { get; set; }
    public string State { get; set; } = ExpenseStates.Recorded;
}

public sealed class Warranty : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string WarrantyKind { get; set; } = "";
    public string Provider { get; set; } = "";
    public Guid? ProviderId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Coverage { get; set; }
    public string? Exclusions { get; set; }
    public string? ResponseTime { get; set; }
    public decimal? Cost { get; set; }
    public Guid? DocumentId { get; set; }
    public string State { get; set; } = WarrantyStates.Active;
}

public sealed class WarrantyClaim : AuditableEntity
{
    public string ClaimNumber { get; set; } = "";
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public DateTime RaisedOn { get; set; }
    public Guid? RaisedById { get; set; }
    public string FaultDescription { get; set; } = "";
    public Guid? WorkOrderId { get; set; }
    public string? ProviderReference { get; set; }
    public decimal? AmountClaimed { get; set; }
    public decimal? AmountRecovered { get; set; }
    public string? Resolution { get; set; }
    public string State { get; set; } = WarrantyClaimStates.Submitted;
    public DateTime? ClosedOn { get; set; }
}

public sealed class DepreciationSchedule : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string Method { get; set; } = "Straight line";
    public decimal AcquisitionValue { get; set; }
    public decimal ResidualValue { get; set; }
    public int UsefulLifeMonths { get; set; }
    public DateTime StartDate { get; set; }
    public decimal? Rate { get; set; }
    public string PeriodLength { get; set; } = "Monthly";
    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
    public string State { get; set; } = DepreciationStates.Running;
    public Guid? SupersededById { get; set; }
    public ICollection<DepreciationEntry> Entries { get; set; } = [];
}

public sealed class DepreciationEntry : Entity
{
    public Guid ScheduleId { get; set; }
    public DepreciationSchedule Schedule { get; set; } = null!;
    public string Period { get; set; } = "";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal OpeningValue { get; set; }
    public decimal Charge { get; set; }
    public decimal ClosingValue { get; set; }
    public string State { get; set; } = "Projected";
}
