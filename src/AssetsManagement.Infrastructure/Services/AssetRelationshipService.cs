using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetRelationshipService(AssetsDbContext db, ICurrentUser currentUser) : IAssetRelationshipService
{
    private const string EntityType = nameof(AssetRelationship);
    private const string Lifecycle = "Active → Ended";
    private const string Source = "Screen";

    public async Task<PagedResult<AssetRelationshipListItemDto>> ListAsync(
        AssetRelationshipListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetRelationships.AsNoTracking().AsQueryable();
        if (query.SourceAssetId.HasValue)
            source = source.Where(x => x.SourceAssetId == query.SourceAssetId);
        if (query.TargetAssetId.HasValue)
            source = source.Where(x => x.TargetAssetId == query.TargetAssetId);
        if (query.AssetId.HasValue)
            source = source.Where(x => x.SourceAssetId == query.AssetId || x.TargetAssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.RelationshipType))
            source = source.Where(x => x.RelationshipType == query.RelationshipType);
        if (!string.IsNullOrWhiteSpace(query.Status))
            source = source.Where(x => x.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.RelationshipType.Contains(search) ||
                (x.Notes != null && x.Notes.Contains(search)) ||
                x.SourceAsset.Name.Contains(search) || x.TargetAsset.Name.Contains(search));
        }
        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("relationshiptype", "desc") => source.OrderByDescending(x => x.RelationshipType),
            ("relationshiptype", _) => source.OrderBy(x => x.RelationshipType),
            ("status", "desc") => source.OrderByDescending(x => x.Status),
            ("status", _) => source.OrderBy(x => x.Status),
            (_, "asc") => source.OrderBy(x => x.ValidFromUtc),
            _ => source.OrderByDescending(x => x.ValidFromUtc)
        };
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetRelationshipListItemDto(
                x.Id, x.SourceAssetId, x.TargetAssetId, x.RelationshipType, x.ValidFromUtc, x.ValidToUtc, x.Status))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query.PageNumber, query.PageSize, total);
    }

    public async Task<AssetRelationshipDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<AssetRelationshipDetailDto> CreateAsync(
        AssetRelationshipRequest request, CancellationToken cancellationToken)
    {
        AssetDataRules.EnsureRelationshipIsNotSelf(request.SourceAssetId!.Value, request.TargetAssetId!.Value);
        await RequireAssetAsync(request.SourceAssetId.Value, cancellationToken);
        await RequireAssetAsync(request.TargetAssetId.Value, cancellationToken);
        var validFrom = request.ValidFrom ?? DateTime.UtcNow;
        var duplicate = await db.AssetRelationships.AnyAsync(x =>
            x.SourceAssetId == request.SourceAssetId && x.TargetAssetId == request.TargetAssetId &&
            x.RelationshipType == request.RelationshipType && x.ValidFromUtc == validFrom, cancellationToken);
        if (duplicate)
            throw new ConflictException("That asset relationship already exists.");
        if (request.RelationshipType == "Contains")
            await EnsureContainsAcyclicAsync(request.SourceAssetId.Value, request.TargetAssetId.Value, cancellationToken);
        var entity = new AssetRelationship
        {
            SourceAssetId = request.SourceAssetId.Value,
            TargetAssetId = request.TargetAssetId.Value,
            RelationshipType = request.RelationshipType!,
            ValidFromUtc = validFrom,
            ValidToUtc = request.ValidTo,
            Quantity = request.Quantity,
            Notes = NullIfEmpty(request.Notes),
            Status = request.ValidTo.HasValue && request.ValidTo < DateTime.UtcNow
                ? RelationshipStatuses.Ended : RelationshipStatuses.Active
        };
        db.AssetRelationships.Add(entity);
        if (entity.RelationshipType == "Contains" && entity.Status == RelationshipStatuses.Active)
        {
            var child = await db.Assets.SingleAsync(x => x.Id == entity.TargetAssetId, cancellationToken);
            child.ParentAssetId = entity.SourceAssetId;
        }
        AddHistory(entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AssetRelationshipDetailDto> UpdateAsync(
        Guid id, AssetRelationshipRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.Status == RelationshipStatuses.Ended)
            throw new DomainRuleException("An ended relationship cannot be edited.");
        AssetDataRules.EnsureRelationshipIsNotSelf(request.SourceAssetId!.Value, request.TargetAssetId!.Value);
        await RequireAssetAsync(request.SourceAssetId.Value, cancellationToken);
        await RequireAssetAsync(request.TargetAssetId.Value, cancellationToken);
        if (request.RelationshipType == "Contains")
            await EnsureContainsAcyclicAsync(request.SourceAssetId.Value, request.TargetAssetId.Value, cancellationToken);
        entity.SourceAssetId = request.SourceAssetId.Value;
        entity.TargetAssetId = request.TargetAssetId.Value;
        entity.RelationshipType = request.RelationshipType!;
        if (request.ValidFrom.HasValue)
            entity.ValidFromUtc = request.ValidFrom.Value;
        entity.ValidToUtc = request.ValidTo;
        entity.Quantity = request.Quantity;
        entity.Notes = NullIfEmpty(request.Notes);
        AddHistory(entity.Id, "Record updated");
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AssetRelationshipDetailDto> EndAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.Status != RelationshipStatuses.Ended)
        {
            entity.Status = RelationshipStatuses.Ended;
            entity.ValidToUtc ??= DateTime.UtcNow;
            AddHistory(entity.Id, "Status changed from 'Active' to 'Ended'");
            if (entity.RelationshipType == "Contains")
            {
                var child = await db.Assets.SingleAsync(x => x.Id == entity.TargetAssetId, cancellationToken);
                if (child.ParentAssetId == entity.SourceAssetId)
                    child.ParentAssetId = null;
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        return Map(entity);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.Status == RelationshipStatuses.Active)
            await EndAsync(id, cancellationToken);
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssetRelationshipDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        entity.IsActive = true;
        entity.DeletedAtUtc = null;
        entity.DeletedBy = null;
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyCollection<AssetRelationshipHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        _ = await FindAsync(id, cancellationToken);
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetRelationshipHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    private async Task EnsureContainsAcyclicAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken)
    {
        var frontier = new Queue<Guid>();
        frontier.Enqueue(targetId);
        var seen = new HashSet<Guid> { targetId };
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == sourceId)
                AssetDataRules.EnsureContainsIsAcyclic(true);
            var children = await db.AssetRelationships.AsNoTracking()
                .Where(x => x.SourceAssetId == current && x.RelationshipType == "Contains" &&
                    x.Status == RelationshipStatuses.Active)
                .Select(x => x.TargetAssetId)
                .ToArrayAsync(cancellationToken);
            foreach (var child in children)
                if (seen.Add(child))
                    frontier.Enqueue(child);
        }
    }

    private async Task RequireAssetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.Assets.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Asset was not found.");
    }

    private async Task<AssetRelationship> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetRelationships.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Asset relationship was not found.");

    private static AssetRelationshipDetailDto Map(AssetRelationship entity) =>
        new(entity.Id, entity.SourceAssetId, entity.TargetAssetId, entity.RelationshipType, entity.ValidFromUtc,
            entity.ValidToUtc, entity.Quantity, entity.Notes, entity.Status, Lifecycle);

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, int page, int size, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)size);
        return new(rows, page, size, total, pages, page > 1, page < pages);
    }

    private void AddHistory(Guid entityId, string change) =>
        db.ChangeHistory.Add(new ChangeHistoryEntry
        {
            EntityType = EntityType, EntityId = entityId, WhenUtc = DateTime.UtcNow,
            Change = change, By = currentUser.DisplayName, Source = Source
        });

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
