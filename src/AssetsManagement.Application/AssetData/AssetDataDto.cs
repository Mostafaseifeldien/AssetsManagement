namespace AssetsManagement.Application;

public sealed record AssetDataDto(
    Guid Id,
    string Code,
    string Name,
    string? AlternateName,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? RowVersion,
    IReadOnlyDictionary<string, object?> Details);
