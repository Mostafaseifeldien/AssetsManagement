namespace AssetsManagement.Domain;

public sealed class ChangeHistoryEntry : Entity
{
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public DateTime WhenUtc { get; set; }
    public string Change { get; set; } = "";
    public string? Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string By { get; set; } = "";
    public string Source { get; set; } = "Screen";
}
