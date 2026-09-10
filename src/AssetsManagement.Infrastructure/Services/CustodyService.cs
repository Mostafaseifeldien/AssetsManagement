using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class CustodyService(AssetsDbContext db, ICurrentUser currentUser) : ICustodyService
{
    private const string EntityType = nameof(CustodyAssignment);
    private const string Lifecycle = "Active → Closed | Disputed";
    private const string Source = "Screen";

    public async Task<PagedResult<CustodyAssignmentListItemDto>> ListAsync(
        CustodyListQuery query, CancellationToken cancellationToken)
    {
        var source = db.CustodyAssignments.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue)
            source = source.Where(x => x.AssetId == query.AssetId);
        if (query.CustodianId.HasValue)
            source = source.Where(x => x.CustodianId == query.CustodianId);
        if (query.BatchId.HasValue)
            source = source.Where(x => x.BatchId == query.BatchId);
        if (!string.IsNullOrWhiteSpace(query.Status))
            source = source.Where(x => x.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => (x.AssignmentReason != null && x.AssignmentReason.Contains(search)) ||
                x.Asset.Name.Contains(search) || x.Asset.AssetNumber.Contains(search));
        }
        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("status", "desc") => source.OrderByDescending(x => x.Status),
            ("status", _) => source.OrderBy(x => x.Status),
            ("custodianid", "desc") => source.OrderByDescending(x => x.CustodianId),
            ("custodianid", _) => source.OrderBy(x => x.CustodianId),
            (_, "asc") => source.OrderBy(x => x.AssignedFromUtc),
            _ => source.OrderByDescending(x => x.AssignedFromUtc)
        };
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new CustodyAssignmentListItemDto(
                x.Id, x.AssetId, x.CustodianId, x.CustodianType, x.AssignedFromUtc, x.AssignedToUtc,
                x.AssignmentReason, x.Status))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query.PageNumber, query.PageSize, total);
    }

    public async Task<CustodyAssignmentDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<CustodyAssignmentDetailDto> CreateAsync(
        CustodyAssignmentRequest request, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(request.AssetId, cancellationToken);
        await RequireCustodianAsync(request.CustodianId, request.CustodianType, cancellationToken);
        var when = request.AssignedFrom ?? DateTime.UtcNow;
        await CloseActiveAsync(asset, when, cancellationToken);
        var entity = new CustodyAssignment
        {
            AssetId = asset.Id,
            CustodianId = request.CustodianId!.Value,
            CustodianType = request.CustodianType!,
            AssignedFromUtc = when,
            AssignedBy = currentUser.UserName,
            AssignmentReason = NullIfEmpty(request.AssignmentReason),
            Acknowledged = request.Acknowledged == true,
            AcknowledgedAtUtc = request.Acknowledged == true ? when : null,
            Status = CustodyStatuses.Active,
            HandoverDocument = NullIfEmpty(request.HandoverDocument)
        };
        db.CustodyAssignments.Add(entity);
        ApplyToAsset(asset, entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Asset set to '{asset.AssetNumber}'");
        AddHistory(entity.Id, $"Custodian set to '{entity.CustodianId}'");
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CustodyAssignmentDetailDto> UpdateAsync(
        Guid id, CustodyAssignmentRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        AssetDataRules.EnsureCustodyIsMutable(entity.Status);
        await RequireCustodianAsync(request.CustodianId, request.CustodianType, cancellationToken);
        Track(entity.Id, "Custodian", entity.CustodianId.ToString(), request.CustodianId.ToString());
        Track(entity.Id, "Assignment Reason", entity.AssignmentReason, NullIfEmpty(request.AssignmentReason));
        entity.CustodianId = request.CustodianId!.Value;
        entity.CustodianType = request.CustodianType!;
        entity.AssignmentReason = NullIfEmpty(request.AssignmentReason);
        entity.HandoverDocument = NullIfEmpty(request.HandoverDocument);
        if (request.AssignedFrom.HasValue)
            entity.AssignedFromUtc = request.AssignedFrom.Value;
        if (request.Acknowledged == true && !entity.Acknowledged)
        {
            entity.Acknowledged = true;
            entity.AcknowledgedAtUtc = DateTime.UtcNow;
            AddHistory(entity.Id, "Acknowledged set to 'Yes'");
        }
        var asset = await db.Assets.SingleAsync(x => x.Id == entity.AssetId, cancellationToken);
        ApplyToAsset(asset, entity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CustodyAssignmentDetailDto> AcknowledgeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        AssetDataRules.EnsureCustodyIsMutable(entity.Status);
        if (!entity.Acknowledged)
        {
            entity.Acknowledged = true;
            entity.AcknowledgedAtUtc = DateTime.UtcNow;
            AddHistory(entity.Id, "Acknowledged set to 'Yes'");
            await db.SaveChangesAsync(cancellationToken);
        }
        return Map(entity);
    }

    public async Task<CustodyAssignmentDetailDto> DisputeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.Status == CustodyStatuses.Disputed)
            return Map(entity);
        AssetDataRules.EnsureCustodyIsMutable(entity.Status);
        entity.Status = CustodyStatuses.Disputed;
        entity.AssignedToUtc = DateTime.UtcNow;
        AddHistory(entity.Id, "Status changed from 'Active' to 'Disputed'");
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CustodyAssignmentDetailDto> CloseAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.Status == CustodyStatuses.Closed)
            return Map(entity);
        entity.Status = CustodyStatuses.Closed;
        entity.AssignedToUtc ??= DateTime.UtcNow;
        AddHistory(entity.Id, "Status changed to 'Closed'");
        AddHistory(entity.Id, $"Assigned To set to '{entity.AssignedToUtc:O}'");
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyCollection<CustodyHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        _ = await FindAsync(id, cancellationToken);
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new CustodyHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PagedResult<CustodyTransferListItemDto>> ListTransfersAsync(
        CustodyListQuery query, CancellationToken cancellationToken)
    {
        var source = db.CustodyTransferBatches.AsNoTracking().AsQueryable();
        if (query.CustodianId.HasValue)
            source = source.Where(x => x.CustodianId == query.CustodianId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.BatchNumber.Contains(search) || x.Reason.Contains(search));
        }
        source = source.OrderByDescending(x => x.AssignedFromUtc);
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new CustodyTransferListItemDto(
                x.Id, x.BatchNumber, x.CustodianId, x.Reason, x.AssignedFromUtc, x.AssetCount, x.AssignedBy))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query.PageNumber, query.PageSize, total);
    }

    public async Task<CustodyTransferDetailDto> GetTransferAsync(Guid id, CancellationToken cancellationToken)
    {
        var batch = await db.CustodyTransferBatches.Include(x => x.Assignments)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Custody transfer was not found.");
        return MapTransfer(batch);
    }

    public async Task<CustodyTransferDetailDto> TransferAsync(
        CustodyTransferRequest request, CancellationToken cancellationToken)
    {
        var assetIds = request.AssetIds!.Distinct().ToArray();
        await RequireCustodianAsync(request.CustodianId, request.CustodianType, cancellationToken);
        var assets = await db.Assets.Where(x => assetIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
        if (assets.Length != assetIds.Length)
            throw new NotFoundException("One or more assets were not found.");
        var when = request.AssignedFrom ?? DateTime.UtcNow;
        var year = when.Year;
        var count = await db.CustodyTransferBatches.CountAsync(x => x.AssignedFromUtc.Year == year, cancellationToken);
        var batch = new CustodyTransferBatch
        {
            BatchNumber = $"CUB-{year}-{(count + 1).ToString().PadLeft(4, '0')}",
            CustodianId = request.CustodianId!.Value,
            CustodianType = request.CustodianType!,
            Reason = request.Reason!.Trim(),
            AssignedFromUtc = when,
            RequireAcknowledgement = request.RequireAcknowledgement != false,
            AssignedBy = currentUser.UserName,
            AssetCount = assets.Length
        };
        db.CustodyTransferBatches.Add(batch);
        foreach (var asset in assets)
        {
            await CloseActiveAsync(asset, when, cancellationToken);
            var assignment = new CustodyAssignment
            {
                AssetId = asset.Id,
                CustodianId = batch.CustodianId,
                CustodianType = batch.CustodianType,
                AssignedFromUtc = when,
                AssignedBy = currentUser.UserName,
                AssignmentReason = batch.Reason,
                Acknowledged = !batch.RequireAcknowledgement,
                AcknowledgedAtUtc = batch.RequireAcknowledgement ? null : when,
                Status = CustodyStatuses.Active,
                Batch = batch
            };
            db.CustodyAssignments.Add(assignment);
            ApplyToAsset(asset, assignment);
            AddHistory(assignment.Id, $"Custody transferred in {batch.BatchNumber}");
        }
        await db.SaveChangesAsync(cancellationToken);
        return MapTransfer(batch);
    }

    private async Task CloseActiveAsync(Asset asset, DateTime when, CancellationToken cancellationToken)
    {
        var current = await db.CustodyAssignments
            .Where(x => x.AssetId == asset.Id && x.Status == CustodyStatuses.Active)
            .ToArrayAsync(cancellationToken);
        foreach (var item in current)
        {
            item.Status = CustodyStatuses.Closed;
            item.AssignedToUtc = when;
            AddHistory(item.Id, "Status changed from 'Active' to 'Closed'");
        }
    }

    private async Task<Asset> RequireAssetAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty)
            throw new DomainRuleException("Asset id is required.");
        return await db.Assets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
    }

    private async Task RequireCustodianAsync(Guid? id, string? type, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty)
            throw new DomainRuleException("Custodian id is required.");
        if (type == CustodyTypes.Employee)
        {
            _ = await db.Employees.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken)
                ? true
                : throw new NotFoundException("Employee was not found.");
        }
    }

    private static void ApplyToAsset(Asset asset, CustodyAssignment assignment)
    {
        asset.CurrentCustodianId = assignment.CustodianId;
        asset.CustodianType = assignment.CustodianType;
        if (assignment.CustodianType == CustodyTypes.Employee)
        {
            // department is copied when the employee record is loaded by the caller if needed
        }
    }

    private async Task<CustodyAssignment> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.CustodyAssignments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Custody assignment was not found.");

    private static CustodyAssignmentDetailDto Map(CustodyAssignment entity) =>
        new(entity.Id, entity.AssetId, entity.CustodianId, entity.CustodianType, entity.AssignedFromUtc,
            entity.AssignedToUtc, entity.AssignedBy, entity.AssignmentReason, entity.Acknowledged,
            entity.AcknowledgedAtUtc, entity.Status, entity.HandoverDocument, entity.BatchId, Lifecycle);

    private static CustodyTransferDetailDto MapTransfer(CustodyTransferBatch batch) =>
        new(batch.Id, batch.BatchNumber, batch.CustodianId, batch.CustodianType, batch.Reason,
            batch.AssignedFromUtc, batch.RequireAcknowledgement, batch.AssignedBy, batch.AssetCount,
            batch.Assignments.Select(x => x.AssetId).ToArray());

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, int page, int size, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)size);
        return new(rows, page, size, total, pages, page > 1, page < pages);
    }

    private void Track(Guid entityId, string field, string? oldValue, string? newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) return;
        AddHistory(entityId, string.IsNullOrWhiteSpace(oldValue)
            ? $"{field} set to '{newValue}'"
            : $"{field} changed from '{oldValue}' to '{newValue}'");
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
