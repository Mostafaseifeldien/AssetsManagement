using AssetsManagement.Domain;
using FluentValidation;

namespace AssetsManagement.Application;

public class AssetChildListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public Guid? AssetId { get; init; }
    public string? State { get; init; }
}

public sealed class AssetExpenseRequest
{
    public Guid? AssetId { get; init; }
    public string? ExpenseKind { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public DateTime? ExpenseDate { get; init; }
    public Guid? SupplierId { get; init; }
    public string? InvoiceNumber { get; init; }
    public bool? Capitalized { get; init; }
    public string? CostCenter { get; init; }
    public Guid? WorkOrderId { get; init; }
    public Guid? DocumentId { get; init; }
    public string? Notes { get; init; }
    public string? State { get; init; }
}

public sealed record AssetExpenseListItemDto(
    Guid Id, Guid AssetId, string ExpenseKind, decimal Amount, string Currency,
    DateTime ExpenseDate, bool Capitalized, string State);

public sealed record AssetExpenseDetailDto(
    Guid Id, Guid AssetId, string ExpenseKind, decimal Amount, string Currency,
    DateTime ExpenseDate, Guid? SupplierId, string? InvoiceNumber, bool Capitalized,
    string? CostCenter, Guid? WorkOrderId, Guid? DocumentId, string? Notes, string State, string Lifecycle);

public sealed class WarrantyRequest
{
    public Guid? AssetId { get; init; }
    public string? WarrantyKind { get; init; }
    public Guid? ProviderId { get; init; }
    public string? ReferenceNumber { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Coverage { get; init; }
    public string? Exclusions { get; init; }
    public string? ResponseTime { get; init; }
    public decimal? Cost { get; init; }
    public Guid? DocumentId { get; init; }
}

public sealed record WarrantyListItemDto(
    Guid Id, Guid AssetId, string WarrantyKind, string Provider, DateTime StartDate,
    DateTime EndDate, string State);

public sealed record WarrantyDetailDto(
    Guid Id, Guid AssetId, string WarrantyKind, Guid? ProviderId, string Provider,
    string? ReferenceNumber, DateTime StartDate, DateTime EndDate, string? Coverage,
    string? Exclusions, string? ResponseTime, decimal? Cost, Guid? DocumentId,
    string State, int DaysRemaining, string Lifecycle);

public sealed class WarrantyClaimRequest
{
    public Guid? AssetId { get; init; }
    public string? FaultDescription { get; init; }
    public Guid? WarrantyId { get; init; }
    public Guid? RaisedById { get; init; }
    public Guid? WorkOrderId { get; init; }
    public string? ProviderReference { get; init; }
    public decimal? AmountClaimed { get; init; }
}

public sealed class WarrantyClaimDecisionRequest
{
    public decimal? AmountRecovered { get; init; }
    public string? Resolution { get; init; }
}

public sealed record WarrantyClaimListItemDto(
    Guid Id, string ClaimNumber, Guid AssetId, Guid WarrantyId, string FaultDescription,
    DateTime RaisedOn, string State);

public sealed record WarrantyClaimDetailDto(
    Guid Id, string ClaimNumber, Guid WarrantyId, Guid AssetId, DateTime RaisedOn,
    Guid? RaisedById, string FaultDescription, Guid? WorkOrderId, string? ProviderReference,
    decimal? AmountClaimed, decimal? AmountRecovered, string? Resolution, string State,
    DateTime? ClosedOn, string Lifecycle);

public sealed class DepreciationScheduleRequest
{
    public Guid? AssetId { get; init; }
    public string? Method { get; init; }
    public decimal? AcquisitionValue { get; init; }
    public decimal? ResidualValue { get; init; }
    public int? UsefulLifeMonths { get; init; }
    public DateTime? StartDate { get; init; }
    public decimal? Rate { get; init; }
    public string? PeriodLength { get; init; }
}

public sealed record DepreciationEntryDto(
    Guid Id, string Period, DateTime PeriodStart, DateTime PeriodEnd,
    decimal OpeningValue, decimal Charge, decimal ClosingValue, string State);

public sealed record DepreciationScheduleListItemDto(
    Guid Id, Guid AssetId, string Method, int UsefulLifeMonths, decimal AcquisitionValue,
    decimal AccumulatedDepreciation, decimal NetBookValue, string State);

public sealed record DepreciationScheduleDetailDto(
    Guid Id, Guid AssetId, string Method, decimal AcquisitionValue, decimal ResidualValue,
    int UsefulLifeMonths, DateTime StartDate, decimal? Rate, string PeriodLength,
    decimal AccumulatedDepreciation, decimal NetBookValue, string State, Guid? SupersededById,
    decimal MonthlyCharge, IReadOnlyCollection<DepreciationEntryDto> Entries, string Lifecycle);

public sealed class MaintenancePlanRequest
{
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? ScopeKind { get; init; }
    public Guid? ScopeId { get; init; }
    public string? TriggerKind { get; init; }
    public string? TaskList { get; init; }
    public int? IntervalMonths { get; init; }
}

public sealed record MaintenancePlanDto(
    Guid Id, string Code, string Name, string ScopeKind, Guid? ScopeId,
    string TriggerKind, string? TaskList, int? IntervalMonths, bool Active);

public sealed class MaintenanceRequestCreate
{
    public Guid? AssetId { get; init; }
    public Guid? RaisedById { get; init; }
    public string? FaultDescription { get; init; }
    public string? Urgency { get; init; }
    public bool? AssetUsable { get; init; }
    public Guid? PhotographId { get; init; }
    public string? LocationAtReport { get; init; }
}

public sealed class MaintenanceRejectRequest
{
    public string? RejectionReason { get; init; }
}

public sealed class MaintenanceConvertRequest
{
    public string? WorkKind { get; init; }
    public string? Priority { get; init; }
    public Guid? AssignedToId { get; init; }
    public DateTime? ScheduledStart { get; init; }
    public DateTime? DueDate { get; init; }
}

public sealed record MaintenanceRequestListItemDto(
    Guid Id, string RequestNumber, Guid AssetId, string AssetName, string FaultDescription,
    string Urgency, string Usable, DateTime RaisedOn, string? RaisedBy, string State);

public sealed record MaintenanceRequestDetailDto(
    Guid Id, string RequestNumber, Guid AssetId, string AssetName, Guid? RaisedById, string? RaisedBy,
    DateTime RaisedOn, string FaultDescription, string Urgency, bool AssetUsable, string Usable,
    Guid? PhotographId, string? LocationAtReport, string State, Guid? TriagedById,
    string? RejectionReason, Guid? WorkOrderId, string Lifecycle);

public sealed class WorkOrderRequest
{
    public Guid? AssetId { get; init; }
    public string? WorkKind { get; init; }
    public string? Source { get; init; }
    public Guid? SourceRequestId { get; init; }
    public string? Priority { get; init; }
    public DateTime? ScheduledStart { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? AssignedToId { get; init; }
    public Guid? ProviderId { get; init; }
    public bool? UnderWarranty { get; init; }
    public bool? AssetOutOfService { get; init; }
    public string? WorkDone { get; init; }
}

public sealed class WorkOrderCompleteRequest
{
    public string? WorkDone { get; init; }
}

public sealed class WorkOrderLineRequest
{
    public string? LineKind { get; init; }
    public string? Description { get; init; }
    public Guid? TechnicianId { get; init; }
    public decimal? Hours { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public bool? Chargeable { get; init; }
}

public sealed record WorkOrderLineDto(
    Guid Id, int LineNumber, string LineKind, string Description, Guid? TechnicianId,
    decimal? Hours, decimal? Quantity, decimal? UnitCost, decimal LineCost, bool Chargeable);

public sealed record WorkOrderListItemDto(
    Guid Id, string WorkOrderNumber, Guid AssetId, string WorkKind, string? WorkDone,
    DateTime? CompletedAt, decimal TotalCost, bool UnderWarranty, string State);

public sealed record WorkOrderDetailDto(
    Guid Id, string WorkOrderNumber, Guid AssetId, string WorkKind, string Source,
    Guid? SourceRequestId, string Priority, DateTime? ScheduledStart, DateTime? DueDate,
    Guid? AssignedToId, Guid? ProviderId, bool UnderWarranty, bool AssetOutOfService,
    DateTime? StartedAt, DateTime? CompletedAt, decimal DowntimeHours, string? WorkDone,
    decimal TotalCost, string State, IReadOnlyCollection<WorkOrderLineDto> Lines, string Lifecycle);

public sealed class AssetInspectionRequest
{
    public Guid? AssetId { get; init; }
    public string? InspectionKind { get; init; }
    public DateTime? InspectedOn { get; init; }
    public Guid? InspectorId { get; init; }
    public string? Condition { get; init; }
    public bool? LocationConfirmed { get; init; }
    public bool? CustodianConfirmed { get; init; }
    public string? Findings { get; init; }
    public Guid? PhotographId { get; init; }
    public string? ActionRequired { get; init; }
    public DateTime? NextDue { get; init; }
}

public sealed record AssetInspectionListItemDto(
    Guid Id, Guid AssetId, string InspectionKind, DateTime InspectedOn,
    string Condition, string? Findings, string ActionRequired);

public sealed record AssetInspectionDetailDto(
    Guid Id, Guid AssetId, string InspectionKind, DateTime InspectedOn, Guid InspectorId,
    string Condition, bool LocationConfirmed, bool CustodianConfirmed, string? Findings,
    Guid? PhotographId, string ActionRequired, Guid? WorkOrderId, DateTime? NextDue, string Lifecycle);

public sealed class AssetPositionRequest
{
    public Guid? AssetId { get; init; }
    public string? FloorPlan { get; init; }
    public string? Room { get; init; }
    public decimal? X { get; init; }
    public decimal? Y { get; init; }
    public string? PositionSource { get; init; }
    public decimal? Confidence { get; init; }
    public string? DerivedFromReader { get; init; }
}

public sealed record AssetPositionDto(
    Guid Id, Guid AssetId, string FloorPlan, string? Room, decimal X, decimal Y,
    string PositionSource, decimal? Confidence, DateTime UpdatedAt, string? UpdatedBy,
    string? DerivedFromReader, bool Valid, bool IsCurrent);

public sealed class ExitAuthorizationRequest
{
    public Guid? AssetId { get; init; }
    public Guid? RequestedById { get; init; }
    public string? Reason { get; init; }
    public string? Destination { get; init; }
    public DateTime? ExpectedReturn { get; init; }
    public Guid? CarrierId { get; init; }
    public string? GateScope { get; init; }
}

public sealed class ExitDecisionRequest
{
    public string? Level { get; init; }
    public Guid? ApproverId { get; init; }
    public string? Decision { get; init; }
}

public sealed record ExitAuthorizationListItemDto(
    Guid Id, string Number, Guid AssetId, string? Reason, string? Destination,
    DateTime RequestedAt, string Status);

public sealed record ExitAuthorizationDetailDto(
    Guid Id, string Number, string Status, Guid AssetId, Guid RequestedById,
    DateTime RequestedAt, string? Reason, string? Destination, DateTime? ExpectedReturn,
    Guid? CarrierId, string? GateScope, DateTime? ValidFrom, DateTime? ValidTo,
    DateTime? UsedAt, DateTime? ReturnedAt,
    Guid? OwnerApproverId, string? OwnerDecision, DateTime? OwnerDecidedAt,
    Guid? ManagerApproverId, string? ManagerDecision, DateTime? ManagerDecidedAt,
    Guid? SecurityApproverId, string? SecurityDecision, DateTime? SecurityDecidedAt,
    string Lifecycle);

public sealed record AssetFinancialsDto(
    Guid AssetId,
    decimal TotalCostOfOwnership,
    decimal WrittenDown,
    decimal BookValueToday,
    decimal MonthlyCharge,
    decimal Purchase,
    decimal CapitalizedAdditions,
    decimal CapitalizedTotal,
    decimal OperatingSpend,
    decimal DepreciableValue,
    int ExpenseCount,
    bool ScheduleNeedsSupersede,
    DateTime? PurchaseDate,
    decimal? PurchaseValue,
    Guid? SupplierId,
    string? CostCenter,
    string? Depreciation,
    WarrantyDetailDto? Warranty,
    bool InWarranty,
    IReadOnlyCollection<AssetExpenseListItemDto> Expenses,
    IReadOnlyCollection<WarrantyClaimListItemDto> Claims);

public sealed record AssetMaintenanceDto(
    Guid AssetId,
    int JobsOnAsset,
    int OpenJobs,
    decimal MaintenanceCost,
    decimal DowntimeHours,
    int PlansCovering,
    IReadOnlyCollection<WorkOrderListItemDto> WorkOrders,
    IReadOnlyCollection<MaintenanceRequestListItemDto> Faults,
    IReadOnlyCollection<AssetInspectionListItemDto> Inspections,
    IReadOnlyCollection<MaintenancePlanDto> Plans);

public sealed record AssetIdentityDto(
    Guid AssetId,
    bool TagAssigned,
    Guid? RfidTagId,
    string? TagIdentifier,
    DateTime? EncodedAt,
    bool BarcodeAssigned,
    Guid? BarcodeId,
    string? BarcodeValue,
    string? Symbology);

public sealed record AssetMovementDto(
    Guid AssetId,
    AssetPositionDto? Current,
    IReadOnlyCollection<AssetPositionDto> History);

public sealed record AssetScreenDocumentDto(
    Guid Id, string Title, string DocumentKind, DateTime? DocumentDate,
    DateTime? ExpiresOn, string? IssuedBy, string? ReferenceNumber, bool Confidential, string State);

public sealed record AssetScreenPhotoDto(
    Guid Id, string File, bool IsPrimary, string? Purpose, string? Caption, DateTime? CapturedAt);

public sealed record AssetScreenDto(
    AssetDetailDto Asset,
    AssetFinancialsDto Financials,
    AssetMaintenanceDto Maintenance,
    AssetIdentityDto Identity,
    AssetMovementDto Movement,
    IReadOnlyCollection<CustodyAssignmentListItemDto> Custody,
    IReadOnlyCollection<AssetScreenDocumentDto> Documents,
    IReadOnlyCollection<AssetScreenPhotoDto> Photographs);

public sealed record SpecAttributeDto(string Name, string Class, string Type, string? Note);

public sealed record AssetSpecificationDto(
    string Id,
    string Name,
    string Purpose,
    string HostingDomain,
    string Identity,
    string Lifecycle,
    IReadOnlyCollection<SpecAttributeDto> Attributes,
    IReadOnlyCollection<string> Relationships,
    IReadOnlyCollection<string> Rules,
    string Audit,
    string ExtensionPoints,
    string Phase);

public sealed record OperationHistoryDto(DateTime When, string Change, string By, string Source);

public interface IAssetOperationsService
{
    Task<AssetScreenDto> GetScreenAsync(Guid assetId, CancellationToken cancellationToken);
    Task<AssetFinancialsDto> GetFinancialsAsync(Guid assetId, CancellationToken cancellationToken);
    Task<AssetMaintenanceDto> GetMaintenanceAsync(Guid assetId, CancellationToken cancellationToken);
    Task<AssetIdentityDto> GetIdentityAsync(Guid assetId, CancellationToken cancellationToken);
    Task<AssetMovementDto> GetMovementsAsync(Guid assetId, CancellationToken cancellationToken);
    Task<AssetPositionDto?> GetMapPositionAsync(Guid assetId, CancellationToken cancellationToken);
    Task<AssetSpecificationDto> GetSpecificationAsync();

    Task<PagedResult<AssetExpenseListItemDto>> ListExpensesAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<AssetExpenseDetailDto> GetExpenseAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetExpenseDetailDto> CreateExpenseAsync(AssetExpenseRequest request, CancellationToken cancellationToken);
    Task<AssetExpenseDetailDto> UpdateExpenseAsync(Guid id, AssetExpenseRequest request, CancellationToken cancellationToken);
    Task<AssetExpenseDetailDto> ReverseExpenseAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OperationHistoryDto>> GetExpenseHistoryAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<WarrantyListItemDto>> ListWarrantiesAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<WarrantyDetailDto> GetWarrantyAsync(Guid id, CancellationToken cancellationToken);
    Task<WarrantyDetailDto> CreateWarrantyAsync(WarrantyRequest request, CancellationToken cancellationToken);
    Task<WarrantyDetailDto> UpdateWarrantyAsync(Guid id, WarrantyRequest request, CancellationToken cancellationToken);
    Task<WarrantyDetailDto> VoidWarrantyAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<WarrantyClaimListItemDto>> ListClaimsAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<WarrantyClaimDetailDto> GetClaimAsync(Guid id, CancellationToken cancellationToken);
    Task<WarrantyClaimDetailDto> CreateClaimAsync(WarrantyClaimRequest request, CancellationToken cancellationToken);
    Task<WarrantyClaimDetailDto> SettleClaimAsync(Guid id, WarrantyClaimDecisionRequest request, CancellationToken cancellationToken);
    Task<WarrantyClaimDetailDto> RejectClaimAsync(Guid id, WarrantyClaimDecisionRequest request, CancellationToken cancellationToken);

    Task<PagedResult<DepreciationScheduleListItemDto>> ListSchedulesAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<DepreciationScheduleDetailDto> GetScheduleAsync(Guid id, CancellationToken cancellationToken);
    Task<DepreciationScheduleDetailDto> GetAssetScheduleAsync(Guid assetId, CancellationToken cancellationToken);
    Task<DepreciationScheduleDetailDto> CreateScheduleAsync(DepreciationScheduleRequest request, CancellationToken cancellationToken);
    Task<DepreciationScheduleDetailDto> SupersedeScheduleAsync(Guid id, DepreciationScheduleRequest request, CancellationToken cancellationToken);

    Task<PagedResult<MaintenancePlanDto>> ListPlansAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<MaintenancePlanDto> GetPlanAsync(Guid id, CancellationToken cancellationToken);
    Task<MaintenancePlanDto> CreatePlanAsync(MaintenancePlanRequest request, CancellationToken cancellationToken);
    Task<MaintenancePlanDto> UpdatePlanAsync(Guid id, MaintenancePlanRequest request, CancellationToken cancellationToken);

    Task<PagedResult<MaintenanceRequestListItemDto>> ListRequestsAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<MaintenanceRequestDetailDto> GetRequestAsync(Guid id, CancellationToken cancellationToken);
    Task<MaintenanceRequestDetailDto> CreateRequestAsync(MaintenanceRequestCreate request, CancellationToken cancellationToken);
    Task<MaintenanceRequestDetailDto> RejectRequestAsync(Guid id, MaintenanceRejectRequest request, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto> ConvertRequestAsync(Guid id, MaintenanceConvertRequest request, CancellationToken cancellationToken);

    Task<PagedResult<WorkOrderListItemDto>> ListWorkOrdersAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto> GetWorkOrderAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto> CreateWorkOrderAsync(WorkOrderRequest request, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto> UpdateWorkOrderAsync(Guid id, WorkOrderRequest request, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto> StartWorkOrderAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto> CompleteWorkOrderAsync(Guid id, WorkOrderCompleteRequest request, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto> AddWorkOrderLineAsync(Guid id, WorkOrderLineRequest request, CancellationToken cancellationToken);

    Task<PagedResult<AssetInspectionListItemDto>> ListInspectionsAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<AssetInspectionDetailDto> GetInspectionAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetInspectionDetailDto> CreateInspectionAsync(AssetInspectionRequest request, CancellationToken cancellationToken);

    Task<PagedResult<AssetPositionDto>> ListPositionsAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<AssetPositionDto> GetPositionAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetPositionDto> CreatePositionAsync(AssetPositionRequest request, CancellationToken cancellationToken);

    Task<PagedResult<ExitAuthorizationListItemDto>> ListExitsAsync(AssetChildListQuery query, CancellationToken cancellationToken);
    Task<ExitAuthorizationDetailDto> GetExitAsync(Guid id, CancellationToken cancellationToken);
    Task<ExitAuthorizationDetailDto> CreateExitAsync(ExitAuthorizationRequest request, CancellationToken cancellationToken);
    Task<ExitAuthorizationDetailDto> DecideExitAsync(Guid id, ExitDecisionRequest request, CancellationToken cancellationToken);
    Task<ExitAuthorizationDetailDto> ReturnExitAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetChildListQueryValidator : AbstractValidator<AssetChildListQuery>
{
    public AssetChildListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class AssetExpenseRequestValidator : AbstractValidator<AssetExpenseRequest>
{
    public AssetExpenseRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.ExpenseKind).NotEmpty().Must(x => ExpenseKinds.All.Contains(x!))
            .WithMessage("Expense kind is not recognised.");
        RuleFor(x => x.Amount).NotNull().GreaterThan(0);
        RuleFor(x => x.Currency).Must(x => x is null || Currencies.All.Contains(x))
            .WithMessage("Currency must be EGP, USD, EUR, SAR or AED.");
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.Capitalized).NotNull();
        RuleFor(x => x.InvoiceNumber).MaximumLength(100);
        RuleFor(x => x.CostCenter).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.State).Must(x => x is null || ExpenseStates.All.Contains(x))
            .WithMessage("State must be Draft, Recorded or Reversed.");
    }
}

public sealed class WarrantyRequestValidator : AbstractValidator<WarrantyRequest>
{
    public WarrantyRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.WarrantyKind).NotEmpty().Must(x => WarrantyKinds.All.Contains(x!))
            .WithMessage("Warranty kind is not recognised.");
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage("Provider id is required.");
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Coverage).MaximumLength(2000);
        RuleFor(x => x.Exclusions).MaximumLength(2000);
        RuleFor(x => x.ResponseTime).MaximumLength(200);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
    }
}

public sealed class WarrantyClaimRequestValidator : AbstractValidator<WarrantyClaimRequest>
{
    public WarrantyClaimRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.FaultDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ProviderReference).MaximumLength(100);
        RuleFor(x => x.AmountClaimed).GreaterThanOrEqualTo(0).When(x => x.AmountClaimed.HasValue);
    }
}

public sealed class DepreciationScheduleRequestValidator : AbstractValidator<DepreciationScheduleRequest>
{
    public DepreciationScheduleRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Method).NotEmpty().Must(x => DepreciationMethods.All.Contains(x!))
            .WithMessage("Depreciation method is not recognised.");
        RuleFor(x => x.AcquisitionValue).NotNull().GreaterThanOrEqualTo(0);
        RuleFor(x => x.ResidualValue).GreaterThanOrEqualTo(0).When(x => x.ResidualValue.HasValue);
        RuleFor(x => x.UsefulLifeMonths).NotNull().InclusiveBetween(1, 1200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.PeriodLength).Must(x => x is null || PeriodLengths.All.Contains(x))
            .WithMessage("Period length must be Monthly, Quarterly or Annual.");
        RuleFor(x => x.Rate).InclusiveBetween(0, 100).When(x => x.Rate.HasValue);
    }
}

public sealed class MaintenancePlanRequestValidator : AbstractValidator<MaintenancePlanRequest>
{
    public MaintenancePlanRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ScopeKind).NotEmpty().Must(x => MaintenancePlanScopeKinds.All.Contains(x!))
            .WithMessage("Scope kind is not recognised.");
        RuleFor(x => x.ScopeId).NotEmpty();
        RuleFor(x => x.TaskList).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.IntervalMonths).InclusiveBetween(1, 120).When(x => x.IntervalMonths.HasValue);
    }
}

public sealed class MaintenanceRequestCreateValidator : AbstractValidator<MaintenanceRequestCreate>
{
    public MaintenanceRequestCreateValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.FaultDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Urgency).Must(x => x is null || MaintenanceUrgencies.All.Contains(x))
            .WithMessage("Urgency must be Low, Normal, High or Asset stopped.");
        RuleFor(x => x.LocationAtReport).MaximumLength(200);
    }
}

public sealed class MaintenanceRejectRequestValidator : AbstractValidator<MaintenanceRejectRequest>
{
    public MaintenanceRejectRequestValidator() =>
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(1000)
            .WithMessage("A rejection reason is required.");
}

public sealed class WorkOrderRequestValidator : AbstractValidator<WorkOrderRequest>
{
    public WorkOrderRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.WorkKind).NotEmpty().Must(x => WorkKinds.All.Contains(x!))
            .WithMessage("Work kind is not recognised.");
        RuleFor(x => x.Source).Must(x => x is null || WorkSources.All.Contains(x))
            .WithMessage("Source is not recognised.");
        RuleFor(x => x.Priority).Must(x => x is null || WorkPriorities.All.Contains(x))
            .WithMessage("Priority must be Low, Normal, High or Critical.");
        RuleFor(x => x.WorkDone).MaximumLength(4000);
    }
}

public sealed class WorkOrderCompleteRequestValidator : AbstractValidator<WorkOrderCompleteRequest>
{
    public WorkOrderCompleteRequestValidator() =>
        RuleFor(x => x.WorkDone).NotEmpty().MaximumLength(4000);
}

public sealed class WorkOrderLineRequestValidator : AbstractValidator<WorkOrderLineRequest>
{
    public WorkOrderLineRequestValidator()
    {
        RuleFor(x => x.LineKind).NotEmpty().Must(x => WorkLineKinds.All.Contains(x!))
            .WithMessage("Line kind is not recognised.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Hours).GreaterThan(0).When(x => x.Hours.HasValue);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue);
    }
}

public sealed class AssetInspectionRequestValidator : AbstractValidator<AssetInspectionRequest>
{
    public AssetInspectionRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.InspectionKind).NotEmpty().Must(x => InspectionKinds.All.Contains(x!))
            .WithMessage("Inspection kind is not recognised.");
        RuleFor(x => x.InspectedOn).NotEmpty();
        RuleFor(x => x.InspectorId).NotEmpty();
        RuleFor(x => x.Condition).NotEmpty().Must(x => InspectionConditions.All.Contains(x!))
            .WithMessage("Condition is not recognised.");
        RuleFor(x => x.ActionRequired).Must(x => x is null || InspectionActions.All.Contains(x))
            .WithMessage("Action required is not recognised.");
        RuleFor(x => x.Findings).MaximumLength(2000);
    }
}

public sealed class AssetPositionRequestValidator : AbstractValidator<AssetPositionRequest>
{
    public AssetPositionRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.FloorPlan).NotEmpty().MaximumLength(200);
        RuleFor(x => x.X).NotNull();
        RuleFor(x => x.Y).NotNull();
        RuleFor(x => x.PositionSource).Must(x => x is null || PositionSources.All.Contains(x))
            .WithMessage("Position source is not recognised.");
        RuleFor(x => x.Room).MaximumLength(200);
        RuleFor(x => x.Confidence).InclusiveBetween(0, 1).When(x => x.Confidence.HasValue);
        RuleFor(x => x.DerivedFromReader).MaximumLength(100);
    }
}

public sealed class ExitAuthorizationRequestValidator : AbstractValidator<ExitAuthorizationRequest>
{
    public ExitAuthorizationRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.RequestedById).NotEmpty();
        RuleFor(x => x.Reason).Must(x => x is null || ExitReasons.All.Contains(x))
            .WithMessage("Reason must be Repair, Loan, Transfer, Demonstration, Disposal or Home use.");
        RuleFor(x => x.Destination).MaximumLength(200);
        RuleFor(x => x.GateScope).MaximumLength(200);
    }
}

public sealed class ExitDecisionRequestValidator : AbstractValidator<ExitDecisionRequest>
{
    public ExitDecisionRequestValidator()
    {
        RuleFor(x => x.Level).NotEmpty().Must(x => ExitApprovalLevels.All.Contains(x!))
            .WithMessage("Level must be Owner, Manager or Security.");
        RuleFor(x => x.ApproverId).NotEmpty();
        RuleFor(x => x.Decision).NotEmpty().Must(x => ApprovalDecisions.All.Contains(x!))
            .WithMessage("Decision must be Approved or Rejected.");
    }
}
