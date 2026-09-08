namespace AssetsManagement.Domain;

public abstract class CodedMasterEntity : AuditableEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AlternateName { get; set; }
    public string? Description { get; set; }
}
