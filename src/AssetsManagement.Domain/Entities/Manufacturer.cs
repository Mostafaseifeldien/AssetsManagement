namespace AssetsManagement.Domain;

public sealed class Manufacturer : CodedMasterEntity
{
    public string? Country { get; set; }
    public string? SupportContact { get; set; }
    public string? Website { get; set; }
}
