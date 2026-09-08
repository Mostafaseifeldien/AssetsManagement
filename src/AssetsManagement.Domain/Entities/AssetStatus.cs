namespace AssetsManagement.Domain;

public sealed class AssetStatus : CodedMasterEntity
{
    public string StatusCategory { get; set; } = "Unknown";
    public string Color { get; set; } = "#6b7280";
    public bool IsOperational { get; set; }
    public bool IsTerminal { get; set; }
    public bool BlocksMovement { get; set; }
    public int DisplayOrder { get; set; }
    public ICollection<AssetStatusTransition> AllowedFrom { get; set; } = [];
    public ICollection<AssetStatusTransition> AllowedTo { get; set; } = [];
}
