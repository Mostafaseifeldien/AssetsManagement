using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class SupplierService(
    AssetsDbContext db,
    ICurrentUser currentUser) : ISupplierService
{
    private const string EntityType = nameof(Supplier);
    private const string Lifecycle = "Draft → Active → Under review → Blocked → Archived";
    private const string Source = "Screen";

    public async Task<PagedResult<SupplierListItemDto>> ListAsync(
        SupplierListQuery query, CancellationToken cancellationToken)
    {
        var source = db.Suppliers.AsNoTracking().AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (!string.IsNullOrWhiteSpace(query.SupplierKind))
            source = source.Where(x => x.SupplierKind == query.SupplierKind);
        if (!string.IsNullOrWhiteSpace(query.Country))
            source = source.Where(x => x.Country == query.Country);
        if (!string.IsNullOrWhiteSpace(query.Rating))
            source = source.Where(x => x.Rating == query.Rating);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) || x.Code.Contains(search) ||
                (x.AlternateName != null && x.AlternateName.Contains(search)) ||
                (x.ContactPerson != null && x.ContactPerson.Contains(search)) ||
                (x.Email != null && x.Email.Contains(search)) ||
                (x.Telephone != null && x.Telephone.Contains(search)) ||
                (x.TaxRegistration != null && x.TaxRegistration.Contains(search)));
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("code", "desc") => source.OrderByDescending(x => x.Code),
            ("code", _) => source.OrderBy(x => x.Code),
            ("name", "desc") => source.OrderByDescending(x => x.Name),
            ("name", _) => source.OrderBy(x => x.Name),
            ("supplierkind", "desc") => source.OrderByDescending(x => x.SupplierKind),
            ("supplierkind", _) => source.OrderBy(x => x.SupplierKind),
            ("contactperson", "desc") => source.OrderByDescending(x => x.ContactPerson),
            ("contactperson", _) => source.OrderBy(x => x.ContactPerson),
            ("telephone", "desc") => source.OrderByDescending(x => x.Telephone),
            ("telephone", _) => source.OrderBy(x => x.Telephone),
            ("email", "desc") => source.OrderByDescending(x => x.Email),
            ("email", _) => source.OrderBy(x => x.Email),
            (_, "desc") => source.OrderByDescending(x => x.Name),
            _ => source.OrderBy(x => x.Name)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new SupplierListItemDto(
                x.Id, x.Code, x.Name,
                x.SupplierKind == "" ? null : x.SupplierKind,
                x.ContactPerson, x.Telephone, x.Email, x.IsActive, x.MoreInformation))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<SupplierDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        MapDetail(await FindAsync(id, cancellationToken));

    public async Task<SupplierDetailDto> CreateAsync(
        SupplierRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueCodeAsync(request.Code, null, cancellationToken);
        var includeMore = request.MoreInformation == true;
        if (includeMore)
        {
            await EnsureUniqueTaxAsync(request.TaxRegistration, null, cancellationToken);
            await EnsureUniqueExternalAsync(request.ExternalIdentifier, null, cancellationToken);
        }
        var entity = new Supplier();
        Apply(entity, request, includeMore);
        db.Suppliers.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Code set to '{entity.Code}'");
        AddHistory(entity.Id, $"Name set to '{entity.Name}'");
        AddHistory(entity.Id, $"Active set to {YesNoParser.Format(entity.IsActive)}");
        if (!string.IsNullOrWhiteSpace(entity.SupplierKind))
            AddHistory(entity.Id, $"Supplier Kind set to '{entity.SupplierKind}'");
        if (!string.IsNullOrWhiteSpace(entity.ContactPerson))
            AddHistory(entity.Id, $"Contact Person set to '{entity.ContactPerson}'");
        if (!string.IsNullOrWhiteSpace(entity.Telephone))
            AddHistory(entity.Id, $"Telephone set to '{entity.Telephone}'");
        if (!string.IsNullOrWhiteSpace(entity.Email))
            AddHistory(entity.Id, $"Email set to '{entity.Email}'");
        if (!string.IsNullOrWhiteSpace(entity.Country))
            AddHistory(entity.Id, $"Country set to '{entity.Country}'");
        if (!string.IsNullOrWhiteSpace(entity.AlternateName))
            AddHistory(entity.Id, $"Alternate Name set to '{entity.AlternateName}'");
        if (!string.IsNullOrWhiteSpace(entity.TaxRegistration))
            AddHistory(entity.Id, $"Tax Registration set to '{entity.TaxRegistration}'");
        if (!string.IsNullOrWhiteSpace(entity.Address))
            AddHistory(entity.Id, "Address updated");
        if (!string.IsNullOrWhiteSpace(entity.PaymentTerms))
            AddHistory(entity.Id, $"Payment Terms set to '{entity.PaymentTerms}'");
        if (!string.IsNullOrWhiteSpace(entity.Rating))
            AddHistory(entity.Id, $"Rating set to '{entity.Rating}'");
        if (!string.IsNullOrWhiteSpace(entity.ExternalIdentifier))
            AddHistory(entity.Id, $"External Identifier set to '{entity.ExternalIdentifier}'");
        await SaveAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task<SupplierDetailDto> UpdateAsync(
        Guid id, SupplierRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, id, cancellationToken);
        var includeMore = request.MoreInformation == true;
        if (includeMore)
        {
            await EnsureUniqueTaxAsync(request.TaxRegistration, id, cancellationToken);
            await EnsureUniqueExternalAsync(request.ExternalIdentifier, id, cancellationToken);
        }

        Track(entity.Id, "Code", entity.Code, request.Code.Trim());
        Track(entity.Id, "Name", entity.Name, request.Name.Trim());
        Track(entity.Id, "Supplier Kind", NullIfEmpty(entity.SupplierKind), NullIfEmpty(request.SupplierKind));
        Track(entity.Id, "Contact Person", entity.ContactPerson, NullIfEmpty(request.ContactPerson));
        Track(entity.Id, "Telephone", entity.Telephone, NullIfEmpty(request.Telephone));
        Track(entity.Id, "Email", entity.Email, NullIfEmpty(request.Email));
        Track(entity.Id, "Country", entity.Country, NullIfEmpty(request.Country));
        Track(entity.Id, "Active", YesNoParser.Format(entity.IsActive),
            YesNoParser.Format(request.Active == true));
        if (includeMore)
        {
            Track(entity.Id, "Alternate Name", entity.AlternateName, NullIfEmpty(request.AlternateName));
            Track(entity.Id, "Tax Registration", entity.TaxRegistration, NullIfEmpty(request.TaxRegistration));
            Track(entity.Id, "Address", entity.Address, NullIfEmpty(request.Address));
            Track(entity.Id, "Payment Terms", entity.PaymentTerms, NullIfEmpty(request.PaymentTerms));
            Track(entity.Id, "Rating", entity.Rating, NullIfEmpty(request.Rating));
            Track(entity.Id, "External Identifier", entity.ExternalIdentifier, NullIfEmpty(request.ExternalIdentifier));
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

    public async Task<SupplierDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
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
        var source = db.Suppliers.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.Name.Contains(term) || x.Code.Contains(term));
        }
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SupplierHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.Suppliers.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Supplier was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new SupplierHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken) =>
        db.Suppliers.AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId), cancellationToken);

    private async Task<Supplier> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Supplier was not found.");

    private static void Apply(Supplier entity, SupplierRequest request, bool includeMoreInformation)
    {
        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.SupplierKind = NullIfEmpty(request.SupplierKind) ?? "";
        entity.ContactPerson = NullIfEmpty(request.ContactPerson);
        entity.Telephone = NullIfEmpty(request.Telephone);
        entity.Email = NullIfEmpty(request.Email);
        entity.Country = NullIfEmpty(request.Country);
        entity.IsActive = request.Active == true;
        entity.MoreInformation = includeMoreInformation;
        if (includeMoreInformation)
        {
            entity.AlternateName = NullIfEmpty(request.AlternateName);
            entity.TaxRegistration = NullIfEmpty(request.TaxRegistration);
            entity.Address = NullIfEmpty(request.Address);
            entity.PaymentTerms = NullIfEmpty(request.PaymentTerms);
            entity.Rating = NullIfEmpty(request.Rating);
            entity.ExternalIdentifier = NullIfEmpty(request.ExternalIdentifier);
        }
        if (entity.IsActive)
        {
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
        }
        else
            entity.DeletedAtUtc ??= DateTime.UtcNow;
    }

    private async Task EnsureUniqueCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();
        if (await db.Suppliers.AnyAsync(
            x => x.Code == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"Code '{trimmed}' already exists.");
    }

    private async Task EnsureUniqueTaxAsync(string? tax, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = NullIfEmpty(tax);
        if (trimmed is null) return;
        if (await db.Suppliers.AnyAsync(
            x => x.TaxRegistration == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"Tax registration '{trimmed}' already exists.");
    }

    private async Task EnsureUniqueExternalAsync(string? external, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = NullIfEmpty(external);
        if (trimmed is null) return;
        if (await db.Suppliers.AnyAsync(
            x => x.ExternalIdentifier == trimmed && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"External identifier '{trimmed}' already exists.");
    }

    private static SupplierDetailDto MapDetail(Supplier entity) =>
        new(entity.Id, entity.Code, entity.Name, NullIfEmpty(entity.SupplierKind),
            entity.ContactPerson, entity.Telephone, entity.Email, entity.Country,
            entity.IsActive, entity.MoreInformation, entity.AlternateName, entity.TaxRegistration,
            entity.Address, entity.PaymentTerms, entity.Rating, entity.ExternalIdentifier, Lifecycle);

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
