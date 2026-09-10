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
