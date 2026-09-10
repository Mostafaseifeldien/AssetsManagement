using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetStatusService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IAssetStatusService
{
    private const string EntityType = nameof(AssetStatus);
    private const string Lifecycle = "Active → Inactive. Never deleted while referenced by history.";
    private const string Source = "Screen";

    public async Task<PagedResult<AssetStatusListItemDto>> ListAsync(
        AssetStatusListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetStatuses.AsNoTracking().AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (query.IsOperational.HasValue)
            source = source.Where(x => x.IsOperational == query.IsOperational.Value);
        if (query.IsTerminal.HasValue)
            source = source.Where(x => x.IsTerminal == query.IsTerminal.Value);
        if (!string.IsNullOrWhiteSpace(query.StatusCategory))
        {
            var category = AssetStatusRequestValidator.NormalizeCategory(query.StatusCategory);
            if (category is not null)
                source = source.Where(x => x.StatusCategory == category);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search) ||
                x.StatusCategory.Contains(search) ||
                (x.AlternateName != null && x.AlternateName.Contains(search)));
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("code", "desc") => source.OrderByDescending(x => x.Code),
            ("code", _) => source.OrderBy(x => x.Code),
            ("name", "desc") => source.OrderByDescending(x => x.Name),
            ("name", _) => source.OrderBy(x => x.Name),
            ("statuscategory", "desc") => source.OrderByDescending(x => x.StatusCategory),
            ("statuscategory", _) => source.OrderBy(x => x.StatusCategory),
            ("color", "desc") => source.OrderByDescending(x => x.Color),
            ("color", _) => source.OrderBy(x => x.Color),
            ("isoperational", "desc") => source.OrderByDescending(x => x.IsOperational),
            ("isoperational", _) => source.OrderBy(x => x.IsOperational),
            ("isterminal", "desc") => source.OrderByDescending(x => x.IsTerminal),
            ("isterminal", _) => source.OrderBy(x => x.IsTerminal),
            (_, "desc") => source.OrderByDescending(x => x.DisplayOrder).ThenByDescending(x => x.Name),
            _ => source.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetStatusListItemDto(
                x.Id, x.Code, x.Name, x.StatusCategory, x.Color, x.IsOperational, x.IsTerminal,
                x.IsActive, x.MoreInformation))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<AssetStatusDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await MapDetailAsync(await FindAsync(id, cancellationToken), cancellationToken);

    public async Task<AssetStatusDetailDto> CreateAsync(
        AssetStatusRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueCodeAsync(request.Code, null, cancellationToken);
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);
        var includeMore = request.MoreInformation == true;
        var entity = new AssetStatus
        {
            DisplayOrder = await NextSortOrderAsync(cancellationToken)
        };
        Apply(entity, request, includeMore);
        db.AssetStatuses.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Code set to '{entity.Code}'");
        AddHistory(entity.Id, $"Name set to '{entity.Name}'");
        AddHistory(entity.Id, $"Status Category set to '{entity.StatusCategory}'");
        AddHistory(entity.Id, $"Color set to '{entity.Color}'");
        AddHistory(entity.Id, $"Is Operational set to {YesNoParser.Format(entity.IsOperational)}");
        AddHistory(entity.Id, $"Is Terminal set to {YesNoParser.Format(entity.IsTerminal)}");
        AddHistory(entity.Id, $"Blocks Movement set to {YesNoParser.Format(entity.BlocksMovement)}");
        AddHistory(entity.Id, $"Active set to {YesNoParser.Format(entity.IsActive)}");
        if (!string.IsNullOrWhiteSpace(entity.AlternateName))
            AddHistory(entity.Id, $"Alternate Name set to '{entity.AlternateName}'");
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<AssetStatusDetailDto> UpdateAsync(
        Guid id, AssetStatusRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, id, cancellationToken);
        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);
        var includeMore = request.MoreInformation == true;
        var category = AssetStatusRequestValidator.NormalizeCategory(request.StatusCategory) ?? entity.StatusCategory;
        var becameTerminal = request.IsTerminal == true && !entity.IsTerminal;

        Track(entity.Id, "Code", entity.Code, request.Code.Trim());
        Track(entity.Id, "Name", entity.Name, request.Name.Trim());
        Track(entity.Id, "Status Category", entity.StatusCategory, category);
        Track(entity.Id, "Color", entity.Color, request.Color?.Trim());
        Track(entity.Id, "Is Operational", YesNoParser.Format(entity.IsOperational),
            YesNoParser.Format(request.IsOperational == true));
        Track(entity.Id, "Is Terminal", YesNoParser.Format(entity.IsTerminal),
            YesNoParser.Format(request.IsTerminal == true));
        if (request.BlocksMovement.HasValue)
            Track(entity.Id, "Blocks Movement", YesNoParser.Format(entity.BlocksMovement),
                YesNoParser.Format(request.BlocksMovement == true));
        Track(entity.Id, "Active", YesNoParser.Format(entity.IsActive),
            YesNoParser.Format(request.Active == true));
        if (includeMore)
            Track(entity.Id, "Alternate Name", entity.AlternateName, NullIfEmpty(request.AlternateName));

        Apply(entity, request, includeMore);
        if (becameTerminal)
        {
            var outgoing = await db.AssetStatusTransitions.Where(x => x.FromStatusId == id).ToListAsync(cancellationToken);
            if (outgoing.Count > 0)
            {
                db.AssetStatusTransitions.RemoveRange(outgoing);
                AddHistory(entity.Id, "Allowed transitions cleared because the status is terminal");
            }
        }
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

    public async Task<AssetStatusDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
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
        var source = db.AssetStatuses.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.Name.Contains(term) || x.Code.Contains(term));
        }
        return await source.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssetStatusHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.AssetStatuses.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Asset status was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetStatusHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken) =>
        db.AssetStatuses.AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId), cancellationToken);

    public async Task<IReadOnlyCollection<LookupDto>> GetAllowedTransitionsAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.AssetStatuses.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Asset status was not found.");
        return await db.AssetStatusTransitions.AsNoTracking().Where(x => x.FromStatusId == id)
            .OrderBy(x => x.ToStatus.Name)
            .Select(x => new LookupDto(x.ToStatusId, x.ToStatus.Code, x.ToStatus.Name))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> SetAllowedTransitionsAsync(
        Guid id, StatusTransitionRequest request, CancellationToken cancellationToken)
    {
        var ids = request.AllowedToStatusIds ?? [];
        if (ids.Contains(id))
            throw new ConflictException("A status cannot transition to itself.");
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive)
            throw new NotFoundException("Asset status was not found.");
        if (entity.IsTerminal && ids.Count > 0)
            throw new ConflictException("A terminal status cannot have outgoing transitions.");
        var distinct = ids.Distinct().ToArray();
        var valid = await db.AssetStatuses.CountAsync(x => distinct.Contains(x.Id) && x.IsActive, cancellationToken);
        if (valid != distinct.Length)
            throw new NotFoundException("One or more target statuses were not found.");
        var existing = await db.AssetStatusTransitions.Where(x => x.FromStatusId == id).ToListAsync(cancellationToken);
        db.AssetStatusTransitions.RemoveRange(existing);
        db.AssetStatusTransitions.AddRange(distinct.Select(x => new AssetStatusTransition
        {
            FromStatusId = id, ToStatusId = x
        }));
        AddHistory(id, distinct.Length == 0
            ? "Allowed transitions cleared"
            : $"Allowed transitions updated ({distinct.Length})");
        await SaveAsync(cancellationToken);
        return await GetAllowedTransitionsAsync(id, cancellationToken);
    }

    private async Task<AssetStatus> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetStatuses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Asset status was not found.");

    private static void Apply(AssetStatus entity, AssetStatusRequest request, bool includeMoreInformation)
    {
        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.StatusCategory = AssetStatusRequestValidator.NormalizeCategory(request.StatusCategory) ?? "Unknown";
        entity.Color = request.Color!.Trim();
        entity.IsOperational = request.IsOperational == true;
        entity.IsTerminal = request.IsTerminal == true;
        if (request.BlocksMovement.HasValue)
            entity.BlocksMovement = request.BlocksMovement.Value;
        entity.IsActive = request.Active == true;
        entity.MoreInformation = includeMoreInformation;
        if (includeMoreInformation)
            entity.AlternateName = NullIfEmpty(request.AlternateName);
        if (entity.IsActive)
        {
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
        }
        else
            entity.DeletedAtUtc ??= DateTime.UtcNow;
    }

    private async Task<int> NextSortOrderAsync(CancellationToken cancellationToken)
    {
        var max = await db.AssetStatuses.MaxAsync(x => (int?)x.DisplayOrder, cancellationToken);
        return (max ?? 0) + 1;
    }

    private async Task EnsureUniqueCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();
        if (await db.AssetStatuses.AnyAsync(
            x => x.Code == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"Code '{trimmed}' already exists.");
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        if (await db.AssetStatuses.AnyAsync(
            x => x.Name == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"An asset status named '{trimmed}' already exists.");
    }

    private async Task<AssetStatusDetailDto> MapDetailAsync(AssetStatus entity, CancellationToken cancellationToken)
    {
        var transitions = await GetAllowedTransitionsAsync(entity.Id, cancellationToken);
        return new(entity.Id, entity.Code, entity.Name, entity.StatusCategory, entity.Color,
            entity.IsOperational, entity.IsTerminal, entity.BlocksMovement, entity.IsActive,
            entity.MoreInformation, entity.AlternateName, entity.DisplayOrder, Lifecycle, transitions);
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
