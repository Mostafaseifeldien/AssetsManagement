using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetImageService(
    AssetsDbContext db,
    IFileStorageService files,
    ICurrentUser currentUser) : IAssetImageService
{
    private const string EntityType = nameof(AssetImage);
    private const string Lifecycle = "Active → Superseded → Deleted (soft)";
    private const string Source = "Screen";
    private const string DamageEvidence = "Damage evidence";
    private const string LockedMessage =
        "A photograph used as evidence in an investigation is locked and cannot be changed or removed.";

    public async Task<PagedResult<AssetImageListItemDto>> ListAsync(
        AssetImageListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetImages.AsNoTracking().Include(x => x.Asset).AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (query.IsPrimary.HasValue)
            source = source.Where(x => x.IsPrimary == query.IsPrimary.Value);
        if (!string.IsNullOrWhiteSpace(query.Purpose))
            source = source.Where(x => x.Purpose == query.Purpose.Trim());
        if (!string.IsNullOrWhiteSpace(query.Asset))
        {
            var asset = query.Asset.Trim();
            if (Guid.TryParse(asset, out var assetId))
                source = source.Where(x => x.AssetId == assetId);
            else
                source = source.Where(x => x.Asset.Name == asset || x.Asset.AssetNumber == asset);
        }
        if (!string.IsNullOrWhiteSpace(query.CreatedBy))
        {
            var createdBy = query.CreatedBy.Trim();
            source = source.Where(x => x.CreatedBy.Contains(createdBy) ||
                (x.CapturedBy != null && x.CapturedBy.Contains(createdBy)));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.OriginalFileName.Contains(search) ||
                (x.Caption != null && x.Caption.Contains(search)) ||
                (x.Purpose != null && x.Purpose.Contains(search)) ||
                x.Asset.Name.Contains(search) || x.Asset.AssetNumber.Contains(search));
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("asset", "desc") => source.OrderByDescending(x => x.Asset.Name),
            ("asset", _) => source.OrderBy(x => x.Asset.Name),
            ("file", "desc") => source.OrderByDescending(x => x.OriginalFileName),
            ("file", _) => source.OrderBy(x => x.OriginalFileName),
            ("isprimary", "desc") => source.OrderByDescending(x => x.IsPrimary),
            ("isprimary", _) => source.OrderBy(x => x.IsPrimary),
            ("purpose", "desc") => source.OrderByDescending(x => x.Purpose),
            ("purpose", _) => source.OrderBy(x => x.Purpose),
            (_, "desc") => source.OrderBy(x => x.IsPrimary).ThenBy(x => x.CapturedAtUtc),
            _ => source.OrderByDescending(x => x.IsPrimary).ThenByDescending(x => x.CapturedAtUtc)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetImageListItemDto(
                x.Id, x.Asset.Name, x.OriginalFileName, x.IsPrimary, x.MoreInformation, x.Purpose))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query.PageNumber, query.PageSize, total);
    }

    public async Task<AssetImageDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        MapDetail(await FindAsync(id, cancellationToken, asNoTracking: true));

    public async Task<AssetImageDetailDto> CreateAsync(
        AssetImageCreateRequest request, ImageUpload upload, CancellationToken cancellationToken)
    {
        var asset = await ResolveAssetAsync(request.Asset, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Purpose) &&
            !AssetImageRequestValidator.IsPurpose(request.Purpose))
            throw new Domain.DomainRuleException(
                "Purpose must be Identification, Condition record, Damage evidence or Nameplate.");
        var includeCaption = request.MoreInformation == true;
        var wantPrimary = request.IsPrimary == true;
        var extension = Path.GetExtension(upload.OriginalFileName);
        var stored = await files.SaveAsync(upload.Content, extension, cancellationToken);
        var entity = new AssetImage
        {
            AssetId = asset.Id,
            StoredFileName = stored,
            OriginalFileName = Path.GetFileName(upload.OriginalFileName),
            ContentType = upload.ContentType,
            SizeBytes = upload.SizeBytes,
            Purpose = NullIfEmpty(request.Purpose),
            Caption = includeCaption ? NullIfEmpty(request.Caption) : null,
            MoreInformation = includeCaption,
            CapturedAtUtc = DateTime.UtcNow,
            CapturedBy = currentUser.DisplayName
        };
        var hasPrimary = await db.AssetImages.AnyAsync(
            x => x.AssetId == asset.Id && x.IsPrimary && x.IsActive, cancellationToken);
        entity.IsPrimary = wantPrimary || !hasPrimary;
        if (entity.IsPrimary)
            await ClearPrimaryAsync(asset.Id, null, cancellationToken);
        db.AssetImages.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Asset set to '{asset.Name}'");
        AddHistory(entity.Id, $"File set to '{entity.OriginalFileName}'");
        AddHistory(entity.Id, $"Is Primary set to {YesNoParser.Format(entity.IsPrimary)}");
        if (!string.IsNullOrWhiteSpace(entity.Purpose))
            AddHistory(entity.Id, $"Purpose set to '{entity.Purpose}'");
        if (!string.IsNullOrWhiteSpace(entity.Caption))
            AddHistory(entity.Id, $"Caption set to '{entity.Caption}'");
        try { await SaveAsync(cancellationToken); }
        catch
        {
            await files.DeleteAsync(stored, cancellationToken);
            throw;
        }
        return MapDetail(await FindAsync(entity.Id, cancellationToken, asNoTracking: true));
    }

    public async Task<AssetImageDetailDto> UpdateAsync(
        Guid id, AssetImageRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, asNoTracking: false);
        EnsureUnlocked(entity);
        var asset = await ResolveAssetAsync(request.Asset, cancellationToken);
        var includeMore = request.MoreInformation == true;
        var wantPrimary = request.IsPrimary == true;
        var oldAssetId = entity.AssetId;

        Track(entity.Id, "Asset", entity.Asset.Name, asset.Name);
        Track(entity.Id, "Is Primary", YesNoParser.Format(entity.IsPrimary), YesNoParser.Format(wantPrimary));
        Track(entity.Id, "Purpose", entity.Purpose, NullIfEmpty(request.Purpose));
        if (includeMore)
            Track(entity.Id, "Caption", entity.Caption, NullIfEmpty(request.Caption));

        entity.AssetId = asset.Id;
        entity.Asset = asset;
        entity.Purpose = NullIfEmpty(request.Purpose);
        entity.MoreInformation = includeMore;
        if (includeMore)
            entity.Caption = NullIfEmpty(request.Caption);
        if (wantPrimary)
        {
            await ClearPrimaryAsync(asset.Id, id, cancellationToken);
            entity.IsPrimary = true;
        }
        else
        {
            entity.IsPrimary = false;
            if (oldAssetId == asset.Id)
                await EnsureAssetHasPrimaryAsync(asset.Id, id, cancellationToken);
        }
        if (oldAssetId != asset.Id)
            await EnsureAssetHasPrimaryAsync(oldAssetId, id, cancellationToken);
        await SaveAsync(cancellationToken);
        return MapDetail(await FindAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<AssetImageDetailDto> SetPrimaryAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, asNoTracking: false);
        if (!entity.IsActive)
            throw new NotFoundException("Asset image was not found.");
        EnsureUnlocked(entity);
        if (!entity.IsPrimary)
        {
            await ClearPrimaryAsync(entity.AssetId, id, cancellationToken);
            entity.IsPrimary = true;
            AddHistory(entity.Id, "Is Primary changed from 'No' to 'Yes'");
            await SaveAsync(cancellationToken);
        }
        return MapDetail(await FindAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, asNoTracking: false);
        if (!entity.IsActive) return;
        EnsureUnlocked(entity);
        var assetId = entity.AssetId;
        var wasPrimary = entity.IsPrimary;
        AddHistory(entity.Id, "Record deleted");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        entity.IsPrimary = false;
        await SaveAsync(cancellationToken);
        if (wasPrimary)
            await EnsureAssetHasPrimaryAsync(assetId, id, cancellationToken);
    }

    public async Task<AssetImageDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, asNoTracking: false);
        if (!entity.IsActive)
        {
            AddHistory(entity.Id, "Record restored");
            entity.IsActive = true;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            var hasPrimary = await db.AssetImages.AnyAsync(
                x => x.AssetId == entity.AssetId && x.IsPrimary && x.IsActive && x.Id != id, cancellationToken);
            if (!hasPrimary)
                entity.IsPrimary = true;
            await SaveAsync(cancellationToken);
        }
        return MapDetail(await FindAsync(id, cancellationToken, asNoTracking: true));
    }

    public async Task<IReadOnlyCollection<AssetImageHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.AssetImages.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Asset image was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetImageHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.AssetImages.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset image was not found.");
        return (await files.OpenReadAsync(entity.StoredFileName, cancellationToken),
            entity.ContentType, entity.OriginalFileName);
    }

    private async Task<AssetImage> FindAsync(Guid id, CancellationToken cancellationToken, bool asNoTracking)
    {
        var source = asNoTracking
            ? db.AssetImages.AsNoTracking().Include(x => x.Asset)
            : db.AssetImages.Include(x => x.Asset);
        return await source.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Asset image was not found.");
    }

    private async Task<Asset> ResolveAssetAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new NotFoundException("Asset was not found.");
        var trimmed = value.Trim();
        Asset? asset;
        if (Guid.TryParse(trimmed, out var id))
            asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        else
            asset = await db.Assets.SingleOrDefaultAsync(
                x => (x.Name == trimmed || x.AssetNumber == trimmed) && x.IsActive, cancellationToken);
        return asset ?? throw new NotFoundException($"Asset '{trimmed}' was not found.");
    }

    private async Task ClearPrimaryAsync(Guid assetId, Guid? excludingId, CancellationToken cancellationToken)
    {
        var current = await db.AssetImages
            .Where(x => x.AssetId == assetId && x.IsPrimary && x.IsActive &&
                (!excludingId.HasValue || x.Id != excludingId))
            .ToListAsync(cancellationToken);
        foreach (var image in current)
        {
            if (IsLocked(image) && excludingId.HasValue)
                throw new ConflictException(LockedMessage);
            image.IsPrimary = false;
        }
    }

    private async Task EnsureAssetHasPrimaryAsync(
        Guid assetId, Guid excludingId, CancellationToken cancellationToken)
    {
        var hasPrimary = await db.AssetImages.AnyAsync(
            x => x.AssetId == assetId && x.IsPrimary && x.IsActive && x.Id != excludingId, cancellationToken);
        if (hasPrimary) return;
        var replacement = await db.AssetImages
            .Where(x => x.AssetId == assetId && x.IsActive && x.Id != excludingId)
            .OrderBy(x => x.CapturedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (replacement is null) return;
        replacement.IsPrimary = true;
        await SaveAsync(cancellationToken);
    }

    private static bool IsLocked(AssetImage entity) =>
        string.Equals(entity.Purpose, DamageEvidence, StringComparison.OrdinalIgnoreCase);

    private static void EnsureUnlocked(AssetImage entity)
    {
        if (IsLocked(entity))
            throw new ConflictException(LockedMessage);
    }

    private static AssetImageDetailDto MapDetail(AssetImage entity) =>
        new(entity.Id, entity.Asset.Name, entity.OriginalFileName,
            entity.IsPrimary, entity.MoreInformation, entity.Purpose, entity.Caption,
            entity.CapturedAtUtc, entity.CapturedBy, IsLocked(entity),
            $"/api/asset-images/{entity.Id}/content", Lifecycle);

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, int pageNumber, int pageSize, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new(rows, pageNumber, pageSize, total, pages, pageNumber > 1, pageNumber < pages);
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
