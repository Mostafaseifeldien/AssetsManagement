namespace AssetsManagement.Domain;

public sealed class Employee : CodedMasterEntity
{
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
}
