using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class RfidTagService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IRfidTagService
{
    private const string EntityType = nameof(RfidTag);
    private const string Lifecycle = "Unassigned → Assigned → Damaged | Replaced → Retired";
    private const string Source = "Screen";

    public async Task<PagedResult<RfidTagListItemDto>> ListAsync(
        RfidTagListQuery query, CancellationToken cancellationToken)
    {
        var source = db.RfidTags.AsNoTracking().Include(x => x.Asset).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.TagType))
            source = source.Where(x => x.TagType == query.TagType.Trim());
        if (!string.IsNullOrWhiteSpace(query.EncodingStandard))
            source = source.Where(x => x.EncodingStandard == query.EncodingStandard.Trim());
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<IdentifierStatus>(query.Status, true, out var status))
            source = source.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Asset))
        {
            var asset = query.Asset.Trim();
            source = source.Where(x => x.Asset != null &&
                (x.Asset.Name == asset || x.Asset.AssetNumber == asset));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (Enum.TryParse<IdentifierStatus>(search, true, out var searchStatus))
            {
                source = source.Where(x => x.TagIdentifier.Contains(search) ||
                    x.TagType.Contains(search) ||
                    x.EncodingStandard.Contains(search) ||
                    x.Status == searchStatus ||
                    (x.Asset != null && (x.Asset.Name.Contains(search) || x.Asset.AssetNumber.Contains(search))));
            }
            else
            {
                source = source.Where(x => x.TagIdentifier.Contains(search) ||
                    x.TagType.Contains(search) ||
                    x.EncodingStandard.Contains(search) ||
                    (x.Asset != null && (x.Asset.Name.Contains(search) || x.Asset.AssetNumber.Contains(search))));
            }
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("tagtype", "desc") => source.OrderByDescending(x => x.TagType),
            ("tagtype", _) => source.OrderBy(x => x.TagType),
            ("encodingstandard", "desc") => source.OrderByDescending(x => x.EncodingStandard),
            ("encodingstandard", _) => source.OrderBy(x => x.EncodingStandard),
            ("status", "desc") => source.OrderByDescending(x => x.Status),
            ("status", _) => source.OrderBy(x => x.Status),
            (_, "desc") => source.OrderByDescending(x => x.TagIdentifier),
            _ => source.OrderBy(x => x.TagIdentifier)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new RfidTagListItemDto(
                x.Id, x.TagIdentifier, x.TagType,
                string.IsNullOrWhiteSpace(x.EncodingStandard) ? null : x.EncodingStandard,
                x.Status.ToString(), x.MoreInformation))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query, total);
    }

    public async Task<RfidTagDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));

    public async Task<RfidTagDetailDto> CreateAsync(RfidTagRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueIdentifierAsync(request.TagIdentifier, null, cancellationToken);
        var status = ParseStatus(request.Status);
        var asset = await ResolveAssetAsync(request.Asset, cancellationToken);
        if (status == IdentifierStatus.Assigned)
            await EnsureAssetHasNoAssignedTagAsync(asset!.Id, null, cancellationToken);

        var entity = new RfidTag();
        Apply(entity, request, status, asset, isNew: true);
        db.RfidTags.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Tag Identifier set to '{entity.TagIdentifier}'");
        AddHistory(entity.Id, $"Tag Type set to '{entity.TagType}'");
        if (!string.IsNullOrWhiteSpace(entity.EncodingStandard))
            AddHistory(entity.Id, $"Encoding Standard set to '{entity.EncodingStandard}'");
        AddHistory(entity.Id, $"Status set to {entity.Status}");
        if (asset is not null)
            AddHistory(entity.Id, $"Asset set to '{asset.Name}'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(entity.Id, cancellationToken, asNoTracking: true));
    }

    public async Task<RfidTagDetailDto> UpdateAsync(
        Guid id, RfidTagRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        await EnsureUniqueIdentifierAsync(request.TagIdentifier, id, cancellationToken);
        var status = ParseStatus(request.Status);
        var asset = await ResolveAssetAsync(request.Asset, cancellationToken);
        EnsureAssetNotMoved(entity, asset);

        if (status == IdentifierStatus.Assigned)
        {
            var assetId = asset?.Id ?? entity.AssetId
                ?? throw new DomainRuleException("Asset is required when the tag is assigned.");
            await EnsureAssetHasNoAssignedTagAsync(assetId, id, cancellationToken);
            asset ??= await db.Assets.SingleAsync(x => x.Id == assetId, cancellationToken);
        }

        Track(entity.Id, "Tag Identifier", entity.TagIdentifier, request.TagIdentifier.Trim());
        Track(entity.Id, "Tag Type", entity.TagType, request.TagType.Trim());
        Track(entity.Id, "Encoding Standard", NullIfEmpty(entity.EncodingStandard), NullIfEmpty(request.EncodingStandard));
        Track(entity.Id, "Status", entity.Status.ToString(), status.ToString());
        Track(entity.Id, "Asset", entity.Asset?.Name, asset?.Name);

        Apply(entity, request, status, asset, isNew: false);
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<RfidTagDetailDto> AssignAsync(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        AssetDataRules.EnsureIdentifierCanBeAssigned(entity.Status, entity.AssetId);
        var asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == assetId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
        await EnsureAssetHasNoAssignedTagAsync(assetId, id, cancellationToken);
        entity.AssetId = asset.Id;
        entity.Status = IdentifierStatus.Assigned;
        entity.EncodedAtUtc = DateTime.UtcNow;
        entity.EncodedBy = currentUser.DisplayName;
        AddHistory(entity.Id, $"Asset set to '{asset.Name}'");
        AddHistory(entity.Id, "Status changed from 'Unassigned' to 'Assigned'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<RfidTagDetailDto> UnassignAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        if (entity.Status != IdentifierStatus.Assigned)
            throw new ConflictException("Only an assigned tag can be unassigned.");
        var assetName = entity.Asset?.Name;
        entity.AssetId = null;
        entity.Status = IdentifierStatus.Unassigned;
        AddHistory(entity.Id, string.IsNullOrWhiteSpace(assetName) ? "Asset cleared" : $"Asset '{assetName}' cleared");
        AddHistory(entity.Id, "Status changed from 'Assigned' to 'Unassigned'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<RfidTagDetailDto> ReplaceAsync(
        Guid id, Guid replacementId, CancellationToken cancellationToken)
    {
        var old = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        if (old.Status != IdentifierStatus.Assigned || old.AssetId is null)
            throw new ConflictException("Only an assigned tag can be replaced.");
        if (id == replacementId)
            throw new ConflictException("A tag cannot replace itself.");
        var replacement = await db.RfidTags.Include(x => x.Asset)
            .SingleOrDefaultAsync(x => x.Id == replacementId, cancellationToken)
            ?? throw new NotFoundException("Replacement RFID tag was not found.");
        AssetDataRules.EnsureIdentifierCanBeAssigned(replacement.Status, replacement.AssetId);

        var assetId = old.AssetId.Value;
        var assetName = old.Asset?.Name ?? assetId.ToString();
        old.Status = IdentifierStatus.Replaced;
        old.ReplacedById = replacementId;
        old.AssetId = null;
        replacement.AssetId = assetId;
        replacement.Status = IdentifierStatus.Assigned;
        replacement.EncodedAtUtc = DateTime.UtcNow;
        replacement.EncodedBy = currentUser.DisplayName;
        AddHistory(old.Id, $"Status changed from 'Assigned' to 'Replaced'");
        AddHistory(old.Id, $"Replaced By set to '{replacement.TagIdentifier}'");
        AddHistory(replacement.Id, $"Asset set to '{assetName}'");
        AddHistory(replacement.Id, "Status changed from 'Unassigned' to 'Assigned'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(replacementId, cancellationToken, asNoTracking: true));
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        if (entity.Status == IdentifierStatus.Assigned)
            throw new ConflictException("Replace the RFID tag before retiring it.");
        if (entity.Status != IdentifierStatus.Retired)
        {
            Track(entity.Id, "Status", entity.Status.ToString(), nameof(IdentifierStatus.Retired));
            entity.Status = IdentifierStatus.Retired;
            entity.RetiredAtUtc ??= DateTime.UtcNow;
        }
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<RfidTagDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        if (!entity.IsActive)
        {
            entity.IsActive = true;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            if (entity.Status == IdentifierStatus.Retired)
            {
                Track(entity.Id, "Status", nameof(IdentifierStatus.Retired), nameof(IdentifierStatus.Unassigned));
                entity.Status = IdentifierStatus.Unassigned;
                entity.RetiredAtUtc = null;
            }
            await SaveAsync(cancellationToken);
        }
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(
        string? search, string? status, CancellationToken cancellationToken)
    {
        var source = db.RfidTags.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<IdentifierStatus>(status, true, out var parsed))
            source = source.Where(x => x.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.TagIdentifier.Contains(term) || x.TagType.Contains(term));
        }
        return await source.OrderBy(x => x.TagIdentifier).Take(50)
            .Select(x => new LookupDto(x.Id, x.TagIdentifier, x.TagIdentifier))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RfidTagHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.RfidTags.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("RFID tag was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new RfidTagHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string tagIdentifier, Guid? excludingId, CancellationToken cancellationToken) =>
        db.RfidTags.AnyAsync(x => x.TagIdentifier == tagIdentifier &&
            (!excludingId.HasValue || x.Id != excludingId), cancellationToken);

    public async Task<PagedResult<RfidWaitingAssetDto>> ListAssetsWaitingAsync(
        RfidTagListQuery query, CancellationToken cancellationToken)
    {
        var assigned = db.RfidTags.Where(t => t.Status == IdentifierStatus.Assigned && t.AssetId != null)
            .Select(t => t.AssetId!.Value);
        var source = db.Assets.AsNoTracking().Include(x => x.AssetType)
            .Where(x => x.IsActive && !assigned.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) || x.AssetNumber.Contains(search) ||
                x.Code.Contains(search) || (x.AssetType != null && x.AssetType.Name.Contains(search)));
        }
        source = source.OrderBy(x => x.Name);
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new RfidWaitingAssetDto(
                x.Id, x.Name, x.AssetNumber, x.AssetType != null ? x.AssetType.Name : null))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query, total);
    }

    private async Task<RfidTag> FindTrackedAsync(Guid id, CancellationToken cancellationToken, bool asNoTracking)
    {
        var source = db.RfidTags.Include(x => x.Asset).Include(x => x.ReplacedBy).AsQueryable();
        if (asNoTracking) source = source.AsNoTracking();
        return await source.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found.");
    }

    private void Apply(RfidTag entity, RfidTagRequest request, IdentifierStatus status, Asset? asset, bool isNew)
    {
        entity.TagIdentifier = request.TagIdentifier.Trim();
        entity.TagType = request.TagType.Trim();
        entity.EncodingStandard = NullIfEmpty(request.EncodingStandard) ?? "";
        entity.MoreInformation = request.MoreInformation == true;
        entity.Status = status;
        if (status == IdentifierStatus.Unassigned)
        {
            entity.AssetId = null;
            entity.Asset = null;
        }
        else if (asset is not null)
        {
            entity.AssetId = asset.Id;
            if (isNew || entity.EncodedAtUtc is null)
            {
                entity.EncodedAtUtc = DateTime.UtcNow;
                entity.EncodedBy = currentUser.DisplayName;
            }
        }
        if (status == IdentifierStatus.Retired)
            entity.RetiredAtUtc ??= DateTime.UtcNow;
        else if (status is IdentifierStatus.Unassigned or IdentifierStatus.Assigned)
            entity.RetiredAtUtc = null;
    }

    private async Task EnsureUniqueIdentifierAsync(
        string tagIdentifier, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = tagIdentifier.Trim();
        if (await db.RfidTags.AnyAsync(
            x => x.TagIdentifier == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException("That RFID tag identifier already exists and cannot be reused.");
    }

    private async Task EnsureAssetHasNoAssignedTagAsync(
        Guid assetId, Guid? excludingTagId, CancellationToken cancellationToken)
    {
        if (await db.RfidTags.AnyAsync(x => x.AssetId == assetId &&
            x.Status == IdentifierStatus.Assigned &&
            (!excludingTagId.HasValue || x.Id != excludingTagId), cancellationToken))
            throw new ConflictException("The asset already has an assigned RFID tag.");
    }

    private static void EnsureAssetNotMoved(RfidTag entity, Asset? requestedAsset)
    {
        if (entity.Status != IdentifierStatus.Assigned || entity.AssetId is null) return;
        if (requestedAsset is null) return;
        if (requestedAsset.Id != entity.AssetId)
            throw new ConflictException(
                "An identifier belongs to one asset and is never quietly moved to another. Replace the tag instead.");
    }

    private async Task<Asset?> ResolveAssetAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        Asset? asset;
        if (Guid.TryParse(trimmed, out var id))
            asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        else
            asset = await db.Assets.SingleOrDefaultAsync(
                x => (x.Name == trimmed || x.AssetNumber == trimmed) && x.IsActive, cancellationToken);
        return asset ?? throw new NotFoundException($"Asset '{trimmed}' was not found.");
    }

    private static IdentifierStatus ParseStatus(string? value)
    {
        if (Enum.TryParse<IdentifierStatus>(value, true, out var status) &&
            RfidTagRequestValidator.Statuses.Contains(status.ToString()))
            return status;
        throw new DomainRuleException("Status must be Unassigned, Assigned, Damaged, Replaced or Retired.");
    }

    private RfidTagDetailDto MapDetail(RfidTag entity) =>
        new(entity.Id, entity.TagIdentifier, entity.TagType,
            string.IsNullOrWhiteSpace(entity.EncodingStandard) ? null : entity.EncodingStandard,
            entity.Status.ToString(), entity.MoreInformation, entity.Asset?.Name, entity.EncodedAtUtc, entity.EncodedBy,
            entity.ReplacedBy?.TagIdentifier, entity.RetiredAtUtc, Lifecycle);

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, RfidTagListQuery query, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    private void Track(Guid entityId, string field, string? oldValue, string? newValue)
    {
        var oldText = oldValue?.Trim();
        var newText = newValue?.Trim();
        if (string.Equals(oldText, newText, StringComparison.Ordinal)) return;
        var change = string.IsNullOrWhiteSpace(oldText)
            ? $"{field} set to '{newText}'"
            : string.IsNullOrWhiteSpace(newText)
                ? $"{field} cleared"
                : $"{field} changed from '{oldText}' to '{newText}'";
        AddHistory(entityId, change);
    }

    private void AddHistory(Guid entityId, string change)
    {
        db.ChangeHistory.Add(new ChangeHistoryEntry
        {
            EntityType = EntityType,
            EntityId = entityId,
            WhenUtc = DateTime.UtcNow,
            Change = change,
            By = currentUser.DisplayName,
            Source = Source
        });
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was changed by another user. Reload it and retry.");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ConflictException("A record with the same unique value already exists.");
        }
    }
}
