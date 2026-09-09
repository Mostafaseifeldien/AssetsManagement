using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class BarcodeService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IBarcodeService
{
    private const string EntityType = nameof(Barcode);
    private const string Lifecycle = "Unassigned → Assigned → Replaced → Retired";
    private const string Source = "Screen";
    private const string SubjectTypeAsset = "Asset";

    public async Task<PagedResult<BarcodeListItemDto>> ListAsync(
        BarcodeListQuery query, CancellationToken cancellationToken)
    {
        var source = db.Barcodes.AsNoTracking().Include(x => x.Asset).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Symbology))
            source = source.Where(x => x.Symbology == query.Symbology.Trim());
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<IdentifierStatus>(query.Status, true, out var status))
            source = source.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(query.SubjectReference))
        {
            var subject = query.SubjectReference.Trim();
            source = source.Where(x => x.Asset != null &&
                (x.Asset.Name == subject || x.Asset.AssetNumber == subject));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (Enum.TryParse<IdentifierStatus>(search, true, out var searchStatus) &&
                BarcodeRequestValidator.Statuses.Contains(searchStatus.ToString()))
            {
                source = source.Where(x => x.Value.Contains(search) ||
                    x.Symbology.Contains(search) ||
                    x.Status == searchStatus ||
                    (x.Asset != null && (x.Asset.Name.Contains(search) || x.Asset.AssetNumber.Contains(search))));
            }
            else
            {
                source = source.Where(x => x.Value.Contains(search) ||
                    x.Symbology.Contains(search) ||
                    (x.Asset != null && (x.Asset.Name.Contains(search) || x.Asset.AssetNumber.Contains(search))));
            }
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("symbology", "desc") => source.OrderByDescending(x => x.Symbology),
            ("symbology", _) => source.OrderBy(x => x.Symbology),
            ("status", "desc") => source.OrderByDescending(x => x.Status),
            ("status", _) => source.OrderBy(x => x.Status),
            (_, "desc") => source.OrderByDescending(x => x.Value),
            _ => source.OrderBy(x => x.Value)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new BarcodeListItemDto(x.Id, x.Value, x.Symbology, x.Status.ToString()))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query, total);
    }

    public async Task<BarcodeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));

    public async Task<BarcodeDetailDto> CreateAsync(BarcodeRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueValueAsync(request.Value, request.Symbology, null, cancellationToken);
        var status = ParseStatus(request.Status);
        var asset = await ResolveAssetAsync(request.SubjectReference, cancellationToken);
        if (status == IdentifierStatus.Assigned)
            await EnsureAssetHasNoAssignedBarcodeAsync(asset!.Id, null, cancellationToken);

        var entity = new Barcode { SubjectType = SubjectTypeAsset };
        Apply(entity, request, status, asset, isNew: true);
        db.Barcodes.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Value set to '{entity.Value}'");
        AddHistory(entity.Id, $"Symbology set to '{entity.Symbology}'");
        AddHistory(entity.Id, $"Status set to {entity.Status}");
        if (asset is not null)
            AddHistory(entity.Id, $"Subject Reference set to '{asset.Name}'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(entity.Id, cancellationToken, asNoTracking: true));
    }

    public async Task<BarcodeDetailDto> UpdateAsync(
        Guid id, BarcodeRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        await EnsureUniqueValueAsync(request.Value, request.Symbology, id, cancellationToken);
        var status = ParseStatus(request.Status);
        var asset = await ResolveAssetAsync(request.SubjectReference, cancellationToken);
        EnsureAssetNotMoved(entity, asset);

        if (status == IdentifierStatus.Assigned)
        {
            var assetId = asset?.Id ?? entity.AssetId
                ?? throw new DomainRuleException("Subject reference is required when the barcode is assigned.");
            await EnsureAssetHasNoAssignedBarcodeAsync(assetId, id, cancellationToken);
            asset ??= await db.Assets.SingleAsync(x => x.Id == assetId, cancellationToken);
        }

        Track(entity.Id, "Value", entity.Value, request.Value.Trim());
        Track(entity.Id, "Symbology", entity.Symbology, request.Symbology.Trim());
        Track(entity.Id, "Status", entity.Status.ToString(), status.ToString());
        Track(entity.Id, "Subject Reference", entity.Asset?.Name, asset?.Name);

        Apply(entity, request, status, asset, isNew: false);
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<BarcodeDetailDto> GenerateAsync(string symbology, CancellationToken cancellationToken)
    {
        if (!BarcodeRequestValidator.Symbologies.Contains(symbology))
            throw new DomainRuleException("Supported symbologies are Code128, Code39, QR, DataMatrix and EAN.");
        string value;
        do value = $"BC-{Guid.NewGuid():N}".ToUpperInvariant()[..12];
        while (await db.Barcodes.AnyAsync(x => x.Value == value && x.Symbology == symbology, cancellationToken));
        return await CreateAsync(new BarcodeRequest
        {
            Value = value, Symbology = symbology, Status = nameof(IdentifierStatus.Unassigned)
        }, cancellationToken);
    }

    public async Task<BarcodeDetailDto> AssignAsync(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        AssetDataRules.EnsureIdentifierCanBeAssigned(entity.Status, entity.AssetId);
        var asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == assetId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
        await EnsureAssetHasNoAssignedBarcodeAsync(assetId, id, cancellationToken);
        entity.AssetId = asset.Id;
        entity.SubjectType = SubjectTypeAsset;
        entity.Status = IdentifierStatus.Assigned;
        AddHistory(entity.Id, $"Subject Reference set to '{asset.Name}'");
        AddHistory(entity.Id, "Status changed from 'Unassigned' to 'Assigned'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<BarcodeDetailDto> UnassignAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        if (entity.Status != IdentifierStatus.Assigned)
            throw new ConflictException("Only an assigned barcode can be unassigned.");
        var assetName = entity.Asset?.Name;
        entity.AssetId = null;
        entity.Status = IdentifierStatus.Unassigned;
        AddHistory(entity.Id, string.IsNullOrWhiteSpace(assetName)
            ? "Subject Reference cleared"
            : $"Subject Reference '{assetName}' cleared");
        AddHistory(entity.Id, "Status changed from 'Assigned' to 'Unassigned'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<BarcodeDetailDto> ReplaceAsync(
        Guid id, Guid replacementId, CancellationToken cancellationToken)
    {
        var old = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        if (old.Status != IdentifierStatus.Assigned || old.AssetId is null)
            throw new ConflictException("Only an assigned barcode can be replaced.");
        if (id == replacementId)
            throw new ConflictException("A barcode cannot replace itself.");
        var replacement = await db.Barcodes.Include(x => x.Asset)
            .SingleOrDefaultAsync(x => x.Id == replacementId, cancellationToken)
            ?? throw new NotFoundException("Replacement barcode was not found.");
        AssetDataRules.EnsureIdentifierCanBeAssigned(replacement.Status, replacement.AssetId);

        var assetId = old.AssetId.Value;
        var assetName = old.Asset?.Name ?? assetId.ToString();
        old.Status = IdentifierStatus.Replaced;
        old.ReplacedById = replacementId;
        old.AssetId = null;
        replacement.AssetId = assetId;
        replacement.SubjectType = SubjectTypeAsset;
        replacement.Status = IdentifierStatus.Assigned;
        AddHistory(old.Id, "Status changed from 'Assigned' to 'Replaced'");
        AddHistory(old.Id, $"Replaced By set to '{replacement.Value}'");
        AddHistory(replacement.Id, $"Subject Reference set to '{assetName}'");
        AddHistory(replacement.Id, "Status changed from 'Unassigned' to 'Assigned'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindTrackedAsync(replacementId, cancellationToken, asNoTracking: true));
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindTrackedAsync(id, cancellationToken, asNoTracking: false);
        if (entity.Status == IdentifierStatus.Assigned)
            throw new ConflictException("Replace the barcode before retiring it.");
        if (entity.Status != IdentifierStatus.Retired)
        {
            Track(entity.Id, "Status", entity.Status.ToString(), nameof(IdentifierStatus.Retired));
            entity.Status = IdentifierStatus.Retired;
        }
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<BarcodeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
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
            }
            await SaveAsync(cancellationToken);
        }
        return MapDetail(await FindTrackedAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(
        string? search, string? status, CancellationToken cancellationToken)
    {
        var source = db.Barcodes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<IdentifierStatus>(status, true, out var parsed))
            source = source.Where(x => x.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.Value.Contains(term) || x.Symbology.Contains(term));
        }
        return await source.OrderBy(x => x.Value).Take(50)
            .Select(x => new LookupDto(x.Id, x.Value, x.Value))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BarcodeHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.Barcodes.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Barcode was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new BarcodeHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string value, string? symbology, Guid? excludingId, CancellationToken cancellationToken)
    {
        var source = db.Barcodes.AsQueryable().Where(x => x.Value == value);
        if (!string.IsNullOrWhiteSpace(symbology))
            source = source.Where(x => x.Symbology == symbology);
        if (excludingId.HasValue)
            source = source.Where(x => x.Id != excludingId);
        return source.AnyAsync(cancellationToken);
    }

    public async Task<PagedResult<BarcodeWaitingAssetDto>> ListAssetsWaitingAsync(
        BarcodeListQuery query, CancellationToken cancellationToken)
    {
        var assigned = db.Barcodes.Where(t => t.Status == IdentifierStatus.Assigned && t.AssetId != null)
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
            .Select(x => new BarcodeWaitingAssetDto(
                x.Id, x.Name, x.AssetNumber, x.AssetType != null ? x.AssetType.Name : null))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query, total);
    }

    private async Task<Barcode> FindTrackedAsync(Guid id, CancellationToken cancellationToken, bool asNoTracking)
    {
        var source = db.Barcodes.Include(x => x.Asset).Include(x => x.ReplacedBy).AsQueryable();
        if (asNoTracking) source = source.AsNoTracking();
        return await source.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Barcode was not found.");
    }

    private void Apply(Barcode entity, BarcodeRequest request, IdentifierStatus status, Asset? asset, bool isNew)
    {
        entity.Value = request.Value.Trim();
        entity.Symbology = request.Symbology.Trim();
        entity.SubjectType = SubjectTypeAsset;
        entity.Status = status;
        if (status == IdentifierStatus.Unassigned)
        {
            entity.AssetId = null;
            entity.Asset = null;
        }
        else if (asset is not null)
            entity.AssetId = asset.Id;
        if (isNew)
            entity.PrintedAtUtc ??= DateTime.UtcNow;
    }

    private async Task EnsureUniqueValueAsync(
        string value, string symbology, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = value.Trim();
        var symbol = symbology.Trim();
        if (await db.Barcodes.AnyAsync(x => x.Value == trimmed && x.Symbology == symbol &&
            (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException("That barcode value and symbology already exist and cannot be reused.");
    }

    private async Task EnsureAssetHasNoAssignedBarcodeAsync(
        Guid assetId, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await db.Barcodes.AnyAsync(x => x.AssetId == assetId &&
            x.Status == IdentifierStatus.Assigned &&
            (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException("The asset already has an assigned barcode.");
    }

    private static void EnsureAssetNotMoved(Barcode entity, Asset? requestedAsset)
    {
        if (entity.Status != IdentifierStatus.Assigned || entity.AssetId is null) return;
        if (requestedAsset is null) return;
        if (requestedAsset.Id != entity.AssetId)
            throw new ConflictException(
                "An identifier belongs to one asset and is never quietly moved to another. Replace the barcode instead.");
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
            BarcodeRequestValidator.Statuses.Contains(status.ToString()))
            return status;
        throw new DomainRuleException("Status must be Unassigned, Assigned, Replaced or Retired.");
    }

    private static BarcodeDetailDto MapDetail(Barcode entity) =>
        new(entity.Id, entity.Value, entity.Symbology, entity.Status.ToString(),
            entity.Asset?.Name, entity.PrintedAtUtc, entity.ReplacedBy?.Value, Lifecycle);

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, BarcodeListQuery query, int total)
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
