using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetTypeService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IAssetTypeService
{
    private const string EntityType = nameof(AssetType);
    private const string Lifecycle = "Active → Inactive";
    private const string Source = "Screen";

    public async Task<PagedResult<AssetTypeListItemDto>> ListAsync(
        AssetTypeListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetTypes.AsNoTracking().AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (!string.IsNullOrWhiteSpace(query.AssetCategory))
        {
            var category = query.AssetCategory.Trim();
            source = source.Where(x => x.AssetCategory != null && x.AssetCategory.Name == category);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search) ||
                (x.AlternateName != null && x.AlternateName.Contains(search)));
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("code", "desc") => source.OrderByDescending(x => x.Code),
            ("code", _) => source.OrderBy(x => x.Code),
            ("assetcategory", "desc") => source.OrderByDescending(x => x.AssetCategory != null ? x.AssetCategory.Name : ""),
            ("assetcategory", _) => source.OrderBy(x => x.AssetCategory != null ? x.AssetCategory.Name : ""),
            ("requiresserialnumber", "desc") => source.OrderByDescending(x => x.RequiresSerialNumber),
            ("requiresserialnumber", _) => source.OrderBy(x => x.RequiresSerialNumber),
            ("defaultstatus", "desc") => source.OrderByDescending(x => x.DefaultStatus != null ? x.DefaultStatus.Name : ""),
            ("defaultstatus", _) => source.OrderBy(x => x.DefaultStatus != null ? x.DefaultStatus.Name : ""),
            ("active", "desc") => source.OrderByDescending(x => x.IsActive),
            ("active", _) => source.OrderBy(x => x.IsActive),
            (_, "desc") => source.OrderByDescending(x => x.Name),
            _ => source.OrderBy(x => x.Name)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetTypeListItemDto(
                x.Id,
                x.Name,
                x.Code,
                x.AssetCategory != null ? x.AssetCategory.Name : null,
                x.RequiresSerialNumber,
                x.DefaultStatus != null ? x.DefaultStatus.Name : null,
                x.IsActive))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<AssetTypeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await MapDetailAsync(await FindAsync(id, cancellationToken), cancellationToken);

    public async Task<AssetTypeDetailDto> CreateAsync(
        AssetTypeRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueCodeAsync(request.Code, null, cancellationToken);
        var category = await ResolveCategoryAsync(request.AssetCategory, cancellationToken);
        var status = await ResolveStatusAsync(request.DefaultStatus, cancellationToken);
        var includeMore = YesNoParser.TryParse(request.MoreInformation) == true;
        var entity = new AssetType();
        Apply(entity, request, category?.Id, status?.Id, includeMore);
        db.AssetTypes.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Name set to '{entity.Name}'");
        AddHistory(entity.Id, $"Code set to '{entity.Code}'");
        if (category is not null)
            AddHistory(entity.Id, $"Asset Category set to '{category.Name}'");
        AddHistory(entity.Id, $"Requires Serial Number set to {YesNoParser.Format(entity.RequiresSerialNumber)}");
        if (status is not null)
            AddHistory(entity.Id, $"Default Status set to '{status.Name}'");
        AddHistory(entity.Id, $"Active set to {YesNoParser.Format(entity.IsActive)}");
        if (includeMore)
            AddMoreInformationHistory(entity);
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<AssetTypeDetailDto> UpdateAsync(
        Guid id, AssetTypeRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, id, cancellationToken);
        var category = await ResolveCategoryAsync(request.AssetCategory, cancellationToken);
        var status = await ResolveStatusAsync(request.DefaultStatus, cancellationToken);
        var includeMore = YesNoParser.TryParse(request.MoreInformation) == true;

        Track(entity.Id, "Name", entity.Name, request.Name.Trim());
        Track(entity.Id, "Code", entity.Code, request.Code.Trim());
        Track(entity.Id, "Asset Category", entity.AssetCategory?.Name, category?.Name);
        Track(entity.Id, "Requires Serial Number", YesNoParser.Format(entity.RequiresSerialNumber),
            YesNoParser.Format(YesNoParser.TryParse(request.RequiresSerialNumber) == true));
        Track(entity.Id, "Default Status", entity.DefaultStatus?.Name, status?.Name);
        Track(entity.Id, "Active", YesNoParser.Format(entity.IsActive),
            YesNoParser.Format(YesNoParser.TryParse(request.Active) == true));
        if (includeMore)
        {
            Track(entity.Id, "Alternate Name", entity.AlternateName, NullIfEmpty(request.AlternateName));
            Track(entity.Id, "Requires RFID Tag", YesNoParser.Format(entity.RequiresRfidTag),
                YesNoParser.Format(YesNoParser.TryParse(request.RequiresRfidTag) == true));
            Track(entity.Id, "Requires Barcode", YesNoParser.Format(entity.RequiresBarcode),
                YesNoParser.Format(YesNoParser.TryParse(request.RequiresBarcode) == true));
            Track(entity.Id, "Permitted Status Transitions", entity.PermittedStatusTransitions,
                NullIfEmpty(request.PermittedStatusTransitions));
            Track(entity.Id, "Custom Attribute Schema", entity.CustomAttributeSchema,
                NullIfEmpty(request.CustomAttributeSchema));
            Track(entity.Id, "Default Depreciation Method", entity.DefaultDepreciationMethod,
                NullIfEmpty(request.DefaultDepreciationMethod));
            Track(entity.Id, "Default Useful Life", entity.DefaultUsefulLifeMonths?.ToString(),
                request.DefaultUsefulLife?.ToString());
            Track(entity.Id, "Numbering Scheme", entity.NumberingFormat, NullIfEmpty(request.NumberingScheme));
        }

        Apply(entity, request, category?.Id, status?.Id, includeMore);
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive) return;
        AddHistory(entity.Id, "Active changed from 'Yes' to 'No'");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<AssetTypeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive)
        {
            AddHistory(entity.Id, "Active changed from 'No' to 'Yes'");
            entity.IsActive = true;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            await SaveAsync(cancellationToken);
        }
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken)
    {
        var source = db.AssetTypes.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssetTypeHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.AssetTypes.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Asset type was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetTypeHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<AssetType> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetTypes.Include(x => x.AssetCategory).Include(x => x.DefaultStatus)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Asset type was not found.");

    private static void Apply(
        AssetType entity, AssetTypeRequest request, Guid? categoryId, Guid? statusId, bool includeMoreInformation)
    {
        entity.Name = request.Name.Trim();
        entity.Code = request.Code.Trim();
        entity.AssetCategoryId = categoryId;
        entity.RequiresSerialNumber = YesNoParser.TryParse(request.RequiresSerialNumber) == true;
        entity.DefaultStatusId = statusId;
        entity.IsActive = YesNoParser.TryParse(request.Active) == true;
        if (includeMoreInformation)
        {
            entity.AlternateName = NullIfEmpty(request.AlternateName);
            entity.RequiresRfidTag = YesNoParser.TryParse(request.RequiresRfidTag) == true;
            entity.RequiresBarcode = YesNoParser.TryParse(request.RequiresBarcode) == true;
            entity.PermittedStatusTransitions = NullIfEmpty(request.PermittedStatusTransitions);
            entity.CustomAttributeSchema = NullIfEmpty(request.CustomAttributeSchema);
            entity.DefaultDepreciationMethod = NullIfEmpty(request.DefaultDepreciationMethod);
            entity.DefaultUsefulLifeMonths = request.DefaultUsefulLife;
            entity.NumberingFormat = NullIfEmpty(request.NumberingScheme);
        }
        if (entity.IsActive)
        {
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
        }
        else
            entity.DeletedAtUtc ??= DateTime.UtcNow;
    }

    private void AddMoreInformationHistory(AssetType entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.AlternateName))
            AddHistory(entity.Id, $"Alternate Name set to '{entity.AlternateName}'");
        if (entity.RequiresRfidTag)
            AddHistory(entity.Id, "Requires RFID Tag set to Yes");
        if (entity.RequiresBarcode)
            AddHistory(entity.Id, "Requires Barcode set to Yes");
        if (!string.IsNullOrWhiteSpace(entity.PermittedStatusTransitions))
            AddHistory(entity.Id, "Permitted Status Transitions updated");
        if (!string.IsNullOrWhiteSpace(entity.CustomAttributeSchema))
            AddHistory(entity.Id, "Custom Attribute Schema updated");
        if (!string.IsNullOrWhiteSpace(entity.DefaultDepreciationMethod))
            AddHistory(entity.Id, $"Default Depreciation Method set to '{entity.DefaultDepreciationMethod}'");
        if (entity.DefaultUsefulLifeMonths.HasValue)
            AddHistory(entity.Id, $"Default Useful Life set to '{entity.DefaultUsefulLifeMonths}'");
        if (!string.IsNullOrWhiteSpace(entity.NumberingFormat))
            AddHistory(entity.Id, $"Numbering Scheme set to '{entity.NumberingFormat}'");
    }

    private async Task EnsureUniqueCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();
        if (await db.AssetTypes.AnyAsync(x => x.Code == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"Code '{trimmed}' already exists.");
    }

    private async Task<AssetCategory?> ResolveCategoryAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        AssetCategory? category;
        if (Guid.TryParse(trimmed, out var id))
            category = await db.AssetCategories.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        else
            category = await db.AssetCategories.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Name == trimmed && x.IsActive, cancellationToken);
        return category ?? throw new NotFoundException($"Asset category '{trimmed}' was not found.");
    }

    private async Task<AssetStatus?> ResolveStatusAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        AssetStatus? status;
        if (Guid.TryParse(trimmed, out var id))
            status = await db.AssetStatuses.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        else
            status = await db.AssetStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == trimmed && x.IsActive, cancellationToken);
        return status ?? throw new NotFoundException($"Default status '{trimmed}' was not found.");
    }

    private async Task<AssetTypeDetailDto> MapDetailAsync(AssetType entity, CancellationToken cancellationToken)
    {
        var categoryName = entity.AssetCategory?.Name;
        if (categoryName is null && entity.AssetCategoryId.HasValue)
            categoryName = await db.AssetCategories.AsNoTracking()
                .Where(x => x.Id == entity.AssetCategoryId)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(cancellationToken);

        var statusName = entity.DefaultStatus?.Name;
        if (statusName is null && entity.DefaultStatusId.HasValue)
            statusName = await db.AssetStatuses.AsNoTracking()
                .Where(x => x.Id == entity.DefaultStatusId)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(cancellationToken);

        return new(
            entity.Id,
            entity.Name,
            entity.Code,
            categoryName,
            YesNoParser.Format(entity.RequiresSerialNumber),
            statusName,
            YesNoParser.Format(entity.IsActive),
            entity.AlternateName,
            YesNoParser.Format(entity.RequiresRfidTag),
            YesNoParser.Format(entity.RequiresBarcode),
            entity.PermittedStatusTransitions,
            entity.CustomAttributeSchema,
            entity.DefaultDepreciationMethod,
            entity.DefaultUsefulLifeMonths,
            entity.NumberingFormat,
            Lifecycle);
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
