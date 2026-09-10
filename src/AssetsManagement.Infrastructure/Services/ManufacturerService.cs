using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class ManufacturerService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IManufacturerService
{
    private const string EntityType = nameof(Manufacturer);
    private const string Lifecycle = "Active → Inactive";
    private const string Source = "Screen";

    public async Task<PagedResult<ManufacturerListItemDto>> ListAsync(
        ManufacturerListQuery query, CancellationToken cancellationToken)
    {
        var source = db.Manufacturers.AsNoTracking().AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
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
            ("active", "desc") => source.OrderByDescending(x => x.IsActive),
            ("active", _) => source.OrderBy(x => x.IsActive),
            (_, "desc") => source.OrderByDescending(x => x.Name),
            _ => source.OrderBy(x => x.Name)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new ManufacturerListItemDto(x.Id, x.Name, x.Code, x.IsActive, x.MoreInformation))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<ManufacturerDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        MapDetail(await FindAsync(id, cancellationToken));

    public async Task<ManufacturerDetailDto> CreateAsync(
        ManufacturerRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, null, cancellationToken);
        var entity = new Manufacturer();
        Apply(entity, request, includeMoreInformation: request.MoreInformation == true);
        db.Manufacturers.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Name set to '{entity.Name}'");
        if (!string.IsNullOrWhiteSpace(entity.Code))
            AddHistory(entity.Id, $"Code set to '{entity.Code}'");
        AddHistory(entity.Id, $"Active set to {YesNoParser.Format(entity.IsActive)}");
        if (!string.IsNullOrWhiteSpace(entity.AlternateName))
            AddHistory(entity.Id, $"Alternate Name set to '{entity.AlternateName}'");
        if (!string.IsNullOrWhiteSpace(entity.Country))
            AddHistory(entity.Id, $"Country set to '{entity.Country}'");
        if (!string.IsNullOrWhiteSpace(entity.SupportContact))
            AddHistory(entity.Id, "Support Contact updated");
        if (!string.IsNullOrWhiteSpace(entity.Website))
            AddHistory(entity.Id, $"Website set to '{entity.Website}'");
        await SaveAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task<ManufacturerDetailDto> UpdateAsync(
        Guid id, ManufacturerRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, id, cancellationToken);
        var includeMore = request.MoreInformation == true;
        var code = string.IsNullOrWhiteSpace(request.Code) ? "" : request.Code.Trim();

        Track(entity.Id, "Name", entity.Name, request.Name.Trim());
        Track(entity.Id, "Code", entity.Code, code);
        Track(entity.Id, "Active", YesNoParser.Format(entity.IsActive),
            YesNoParser.Format(request.Active == true));
        if (includeMore)
        {
            Track(entity.Id, "Alternate Name", entity.AlternateName, NullIfEmpty(request.AlternateName));
            Track(entity.Id, "Country", entity.Country, NullIfEmpty(request.Country));
            Track(entity.Id, "Support Contact", entity.SupportContact, NullIfEmpty(request.SupportContact));
            Track(entity.Id, "Website", entity.Website, NullIfEmpty(request.Website));
        }

        Apply(entity, request, includeMore);
        await SaveAsync(cancellationToken);
        return MapDetail(entity);
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

    public async Task<ManufacturerDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
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
        return MapDetail(entity);
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken)
    {
        var source = db.Manufacturers.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ManufacturerHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.Manufacturers.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Manufacturer was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new ManufacturerHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<Manufacturer> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Manufacturers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Manufacturer was not found.");

    private static void Apply(Manufacturer entity, ManufacturerRequest request, bool includeMoreInformation)
    {
        entity.Name = request.Name.Trim();
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? "" : request.Code.Trim();
        entity.IsActive = request.Active == true;
        entity.MoreInformation = includeMoreInformation;
        if (includeMoreInformation)
        {
            entity.AlternateName = NullIfEmpty(request.AlternateName);
            entity.Country = NullIfEmpty(request.Country);
            entity.SupportContact = NullIfEmpty(request.SupportContact);
            entity.Website = NullIfEmpty(request.Website);
        }
        if (entity.IsActive)
        {
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
        }
        else
            entity.DeletedAtUtc ??= DateTime.UtcNow;
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        if (await db.Manufacturers.AnyAsync(x => x.Name == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"A manufacturer named '{trimmed}' already exists.");
    }

    private async Task EnsureUniqueCodeAsync(string? code, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;
        if (await db.Manufacturers.AnyAsync(x => x.Code == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"Code '{trimmed}' already exists.");
    }

    private static ManufacturerDetailDto MapDetail(Manufacturer entity) =>
        new(entity.Id, entity.Name, entity.Code, entity.IsActive, entity.MoreInformation,
            entity.AlternateName, entity.Country, entity.SupportContact, entity.Website, Lifecycle);

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
