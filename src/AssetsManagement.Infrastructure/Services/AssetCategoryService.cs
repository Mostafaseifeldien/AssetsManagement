using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetCategoryService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IAssetCategoryService
{
    private const string EntityType = nameof(AssetCategory);
    private const string Lifecycle = "Active → Inactive";
    private const string Source = "Screen";

    public async Task<PagedResult<AssetCategoryListItemDto>> ListAsync(
        AssetCategoryListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetCategories.AsNoTracking().AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search) ||
                (x.AlternateName != null && x.AlternateName.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(query.ParentCategory))
        {
            var parentName = query.ParentCategory.Trim();
            source = source.Where(x => x.Parent != null && x.Parent.Name == parentName);
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("code", "desc") => source.OrderByDescending(x => x.Code),
            ("code", _) => source.OrderBy(x => x.Code),
            ("active", "desc") => source.OrderByDescending(x => x.IsActive),
            ("active", _) => source.OrderBy(x => x.IsActive),
            (_, "desc") => source.OrderByDescending(x => x.Name),
            _ => source.OrderBy(x => x.Name)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetCategoryListItemDto(x.Id, x.Name, string.IsNullOrWhiteSpace(x.Code) ? null : x.Code, x.IsActive))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<AssetCategoryDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await MapDetailAsync(await FindAsync(id, cancellationToken), cancellationToken);

    public async Task<AssetCategoryDetailDto> CreateAsync(
        AssetCategoryRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, null, cancellationToken);
        var parent = await ResolveParentAsync(request.ParentCategory, null, cancellationToken);
        var entity = new AssetCategory();
        Apply(entity, request, parent?.Id);
        db.AssetCategories.Add(entity);
        AddHistory(entity.Id, "Record created", "Name", null, entity.Name);
        if (!string.IsNullOrWhiteSpace(entity.Code))
            AddHistory(entity.Id, $"Code set to '{entity.Code}'", "Code", null, entity.Code);
        AddHistory(entity.Id, $"Active set to {(entity.IsActive ? "Yes" : "No")}", "Active", null, YesNo(entity.IsActive));
        if (!string.IsNullOrWhiteSpace(entity.AlternateName))
            AddHistory(entity.Id, $"Alternate Name set to '{entity.AlternateName}'", "Alternate Name", null, entity.AlternateName);
        if (parent is not null)
            AddHistory(entity.Id, $"Parent Category set to '{parent.Name}'", "Parent Category", null, parent.Name);
        if (!string.IsNullOrWhiteSpace(entity.AccountCode))
            AddHistory(entity.Id, $"Account Code set to '{entity.AccountCode}'", "Account Code", null, entity.AccountCode);
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<AssetCategoryDetailDto> UpdateAsync(
        Guid id, AssetCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, id, cancellationToken);
        var parent = await ResolveParentAsync(request.ParentCategory, id, cancellationToken);
        AssetDataRules.EnsureCategoryParentIsValid(entity.Id, parent?.Id);
        await EnsureNoCycleAsync(entity.Id, parent?.Id, cancellationToken);

        Track(entity.Id, "Name", entity.Name, request.Name.Trim());
        Track(entity.Id, "Code", NullIfEmpty(entity.Code), NullIfEmpty(request.Code));
        Track(entity.Id, "Active", YesNo(entity.IsActive), YesNo(request.Active == true));
        Track(entity.Id, "Alternate Name", entity.AlternateName, NullIfEmpty(request.AlternateName));
        Track(entity.Id, "Parent Category", entity.Parent?.Name, parent?.Name);
        Track(entity.Id, "Account Code", entity.AccountCode, NullIfEmpty(request.AccountCode));

        Apply(entity, request, parent?.Id);
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive) return;
        Track(entity.Id, "Active", "Yes", "No");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<AssetCategoryDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive)
        {
            Track(entity.Id, "Active", "No", "Yes");
            entity.IsActive = true;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            await SaveAsync(cancellationToken);
        }
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken)
    {
        var source = db.AssetCategories.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssetCategoryHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.AssetCategories.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Asset category was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetCategoryHistoryDto(
                x.WhenUtc, x.Change, x.Field, x.OldValue, x.NewValue, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<AssetCategory> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetCategories.Include(x => x.Parent).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Asset category was not found.");

    private static void Apply(AssetCategory entity, AssetCategoryRequest request, Guid? parentId)
    {
        entity.Name = request.Name.Trim();
        entity.Code = request.Code?.Trim() ?? "";
        entity.IsActive = request.Active == true;
        entity.AlternateName = NullIfEmpty(request.AlternateName);
        entity.ParentId = parentId;
        entity.AccountCode = NullIfEmpty(request.AccountCode);
        if (entity.IsActive)
        {
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
        }
        else
        {
            entity.DeletedAtUtc ??= DateTime.UtcNow;
        }
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        if (await db.AssetCategories.AnyAsync(x => x.Name == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"An asset category named '{trimmed}' already exists.");
    }

    private async Task EnsureUniqueCodeAsync(string? code, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;
        if (await db.AssetCategories.AnyAsync(x => x.Code == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"Code '{trimmed}' already exists.");
    }

    private async Task<AssetCategory?> ResolveParentAsync(
        string? parentName, Guid? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parentName)) return null;
        var parent = await db.AssetCategories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == parentName.Trim() && x.IsActive, cancellationToken)
            ?? throw new NotFoundException($"Parent category '{parentName.Trim()}' was not found.");
        if (currentId.HasValue)
            AssetDataRules.EnsureCategoryParentIsValid(currentId.Value, parent.Id);
        return parent;
    }

    private async Task EnsureNoCycleAsync(Guid categoryId, Guid? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue) return;
        var seen = new HashSet<Guid> { categoryId };
        var currentId = parentId;
        while (currentId.HasValue)
        {
            if (!seen.Add(currentId.Value))
                throw new ConflictException("The category hierarchy would contain a cycle.");
            var parent = await db.AssetCategories.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == currentId, cancellationToken)
                ?? throw new NotFoundException("A category ancestor was not found.");
            currentId = parent.ParentId;
        }
    }

    private async Task<AssetCategoryDetailDto> MapDetailAsync(AssetCategory entity, CancellationToken cancellationToken)
    {
        var parentName = entity.Parent?.Name;
        if (parentName is null && entity.ParentId.HasValue)
            parentName = await db.AssetCategories.AsNoTracking()
                .Where(x => x.Id == entity.ParentId)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(cancellationToken);

        var types = await db.AssetTypes.AsNoTracking()
            .Where(x => x.AssetCategoryId == entity.Id)
            .OrderBy(x => x.Name)
            .Select(x => new AssetCategoryTypeItemDto(x.Name, x.Code, x.IsActive))
            .ToArrayAsync(cancellationToken);

        return new(
            entity.Id,
            entity.Name,
            NullIfEmpty(entity.Code),
            entity.IsActive,
            entity.AlternateName,
            parentName,
            entity.AccountCode,
            Lifecycle,
            types);
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
        AddHistory(entityId, change, field, oldText, newText);
    }

    private void AddHistory(Guid entityId, string change, string field, string? oldValue, string? newValue)
    {
        db.ChangeHistory.Add(new ChangeHistoryEntry
        {
            EntityType = EntityType,
            EntityId = entityId,
            WhenUtc = DateTime.UtcNow,
            Change = change,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
            By = currentUser.DisplayName,
            Source = Source
        });
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string YesNo(bool value) => value ? "Yes" : "No";

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
