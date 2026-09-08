namespace AssetsManagement.Application;

public sealed class ImageUpload
{
    public required Stream Content { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public string? Caption { get; init; }
    public string? Purpose { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed record AssetImageDto(
    Guid Id, Guid AssetId, string AssetName, string OriginalFileName, string ContentType,
    long SizeBytes, string? Caption, string? Purpose, bool IsPrimary, bool IsLocked, string ContentUrl,
    DateTime CreatedAtUtc);
