using AssetsManagement.Domain;
using FluentValidation;

namespace AssetsManagement.Application;

public sealed class AssetRelationshipListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public Guid? SourceAssetId { get; init; }
    public Guid? TargetAssetId { get; init; }
    public Guid? AssetId { get; init; }
    public string? RelationshipType { get; init; }
    public string? Status { get; init; }
}

public sealed class AssetRelationshipRequest
{
    public Guid? SourceAssetId { get; init; }
    public Guid? TargetAssetId { get; init; }
    public string? RelationshipType { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public decimal? Quantity { get; init; }
    public string? Notes { get; init; }
}

public sealed record AssetRelationshipListItemDto(
    Guid Id,
    Guid SourceAssetId,
    Guid TargetAssetId,
    string RelationshipType,
    DateTime ValidFrom,
    DateTime? ValidTo,
    string Status);

public sealed record AssetRelationshipDetailDto(
    Guid Id,
    Guid SourceAssetId,
    Guid TargetAssetId,
    string RelationshipType,
    DateTime ValidFrom,
    DateTime? ValidTo,
    decimal? Quantity,
    string? Notes,
    string Status,
    string Lifecycle);

public sealed record AssetRelationshipHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IAssetRelationshipService
{
    Task<PagedResult<AssetRelationshipListItemDto>> ListAsync(
        AssetRelationshipListQuery query, CancellationToken cancellationToken);
    Task<AssetRelationshipDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetRelationshipDetailDto> CreateAsync(AssetRelationshipRequest request, CancellationToken cancellationToken);
    Task<AssetRelationshipDetailDto> UpdateAsync(Guid id, AssetRelationshipRequest request, CancellationToken cancellationToken);
    Task<AssetRelationshipDetailDto> EndAsync(Guid id, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetRelationshipDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetRelationshipHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetRelationshipListQueryValidator : AbstractValidator<AssetRelationshipListQuery>
{
    public AssetRelationshipListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
        RuleFor(x => x.RelationshipType).Must(x => x is null || RelationshipTypes.All.Contains(x))
            .WithMessage("Relationship type is not recognised.");
        RuleFor(x => x.Status).Must(x => x is null || RelationshipStatuses.All.Contains(x))
            .WithMessage("Status must be Active or Ended.");
    }
}

public sealed class AssetRelationshipRequestValidator : AbstractValidator<AssetRelationshipRequest>
{
    public AssetRelationshipRequestValidator()
    {
        RuleFor(x => x.SourceAssetId).NotEmpty().WithMessage("Source asset id is required.");
        RuleFor(x => x.TargetAssetId).NotEmpty().WithMessage("Target asset id is required.");
        RuleFor(x => x.RelationshipType).NotEmpty().Must(x => RelationshipTypes.All.Contains(x!))
            .WithMessage("Relationship type must be Contains, Connected To, Mounted On, Backup For, Accessory Of, Spare For or Replaces.");
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue);
    }
}
