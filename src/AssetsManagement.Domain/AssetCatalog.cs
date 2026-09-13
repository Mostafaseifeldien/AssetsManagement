namespace AssetsManagement.Domain;

public static class LocationSources
{
    public static readonly string[] All = ["Manual", "Inventory", "Reader", "Operation"];
}

public static class CustodyTypes
{
    public const string Employee = "Employee";
    public const string Contractor = "Contractor";
    public const string Department = "Department";
    public const string Team = "Team";
    public static readonly string[] All = [Employee, Contractor, Department, Team];
}

public static class CustodyStatuses
{
    public const string Active = "Active";
    public const string Closed = "Closed";
    public const string Disputed = "Disputed";
    public static readonly string[] All = [Active, Closed, Disputed];
}

public static class RelationshipTypes
{
    public static readonly string[] All =
        ["Contains", "Connected To", "Mounted On", "Backup For", "Accessory Of", "Spare For", "Replaces"];
}

public static class RelationshipStatuses
{
    public const string Active = "Active";
    public const string Ended = "Ended";
    public static readonly string[] All = [Active, Ended];
}

public static class DocumentKinds
{
    public static readonly string[] All =
    [
        "Invoice", "Purchase contract", "Warranty certificate", "Calibration certificate",
        "Manual", "Insurance policy", "Disposal record", "Other"
    ];
}

public static class DocumentStates
{
    public const string Uploaded = "Uploaded";
    public const string Current = "Current";
    public const string Superseded = "Superseded";
    public const string Expired = "Expired";
    public const string Archived = "Archived";
    public static readonly string[] All = [Uploaded, Current, Superseded, Expired, Archived];
}

public static class Criticalities
{
    public static readonly string[] All = ["High", "Medium", "Low"];
}

public static class ExpenseKinds
{
    public static readonly string[] All =
        ["Purchase", "Shipping", "Customs", "Installation", "Repair", "Service contract", "Insurance", "Consumable", "Other"];
}

public static class ExpenseStates
{
    public const string Draft = "Draft";
    public const string Recorded = "Recorded";
    public const string Reversed = "Reversed";
    public static readonly string[] All = [Draft, Recorded, Reversed];
}

public static class DepreciationMethods
{
    public static readonly string[] All = ["Straight line", "Reducing balance", "Units of production", "Not depreciated"];
}

public static class DepreciationStates
{
    public const string Draft = "Draft";
    public const string Running = "Running";
    public const string Suspended = "Suspended";
    public const string Completed = "Completed";
    public const string Superseded = "Superseded";
    public static readonly string[] All = [Draft, Running, Suspended, Completed, Superseded];
}

public static class WarrantyKinds
{
    public static readonly string[] All = ["Manufacturer", "Extended", "Service contract", "Insurance"];
}

public static class WarrantyStates
{
    public const string Active = "Active";
    public const string Expiring = "Expiring";
    public const string Expired = "Expired";
    public const string Void = "Void";
    public static readonly string[] All = [Active, Expiring, Expired, Void];
}

public static class WarrantyClaimStates
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string Acknowledged = "Acknowledged";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Settled = "Settled";
    public const string Withdrawn = "Withdrawn";
    public static readonly string[] All = [Draft, Submitted, Acknowledged, Approved, Rejected, Settled, Withdrawn];
}

public static class MaintenanceUrgencies
{
    public static readonly string[] All = ["Low", "Normal", "High", "Asset stopped"];
}

public static class MaintenanceRequestStates
{
    public const string Submitted = "Submitted";
    public const string Triaged = "Triaged";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Converted = "Converted";
    public const string Withdrawn = "Withdrawn";
    public static readonly string[] All = [Submitted, Triaged, Accepted, Rejected, Converted, Withdrawn];
}

public static class WorkKinds
{
    public static readonly string[] All = ["Preventive", "Corrective", "Inspection", "Calibration", "Modification", "Emergency"];
}

public static class WorkOrderStates
{
    public const string Draft = "Draft";
    public const string Scheduled = "Scheduled";
    public const string InProgress = "In progress";
    public const string OnHold = "On hold";
    public const string Completed = "Completed";
    public const string Verified = "Verified";
    public const string Canceled = "Canceled";
    public static readonly string[] Open = [Draft, Scheduled, InProgress, OnHold];
    public static readonly string[] All = [Draft, Scheduled, InProgress, OnHold, Completed, Verified, Canceled];
}

public static class InspectionKinds
{
    public static readonly string[] All = ["Condition", "Safety", "Compliance", "Verification of existence", "Handover"];
}

public static class InspectionConditions
{
    public static readonly string[] All = ["Excellent", "Good", "Fair", "Poor", "Unserviceable"];
}

public static class InspectionActions
{
    public static readonly string[] All = ["None", "Maintenance", "Replacement", "Disposal", "Investigation"];
}

public static class ExitReasons
{
    public static readonly string[] All = ["Repair", "Loan", "Transfer", "Demonstration", "Disposal", "Home use"];
}

public static class ExitAuthorizationStates
{
    public const string Draft = "Draft";
    public const string PendingOwner = "Pending Owner";
    public const string PendingManager = "Pending Manager";
    public const string PendingSecurity = "Pending Security";
    public const string Approved = "Approved";
    public const string Active = "Active";
    public const string Used = "Used";
    public const string Expired = "Expired";
    public const string Rejected = "Rejected";
    public const string Revoked = "Revoked";
    public static readonly string[] All =
        [Draft, PendingOwner, PendingManager, PendingSecurity, Approved, Active, Used, Expired, Rejected, Revoked];
}

public static class PositionSources
{
    public static readonly string[] All = ["Manual", "Reader-derived", "RTLS", "Inferred from location"];
}

public static class Currencies
{
    public static readonly string[] All = ["EGP", "USD", "EUR", "SAR", "AED"];
}

public static class PeriodLengths
{
    public static readonly string[] All = ["Monthly", "Quarterly", "Annual"];
}

public static class WorkSources
{
    public static readonly string[] All = ["Maintenance plan", "Maintenance request", "Inspection finding", "Manual"];
}

public static class WorkPriorities
{
    public static readonly string[] All = ["Low", "Normal", "High", "Critical"];
}

public static class WorkLineKinds
{
    public static readonly string[] All = ["Labor", "Spare part", "External service", "Consumable", "Travel"];
}

public static class MaintenancePlanScopeKinds
{
    public static readonly string[] All = ["Single asset", "Asset type", "Asset category", "Location"];
}

public static class ApprovalDecisions
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public static readonly string[] All = [Approved, Rejected];
}

public static class ExitApprovalLevels
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Security = "Security";
    public static readonly string[] All = [Owner, Manager, Security];
}
