using System.Text;
using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetModelService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IAssetModelService
{
    private const string EntityType = nameof(AssetModel);
    private const string Lifecycle = "Active → Inactive";
    private const string Source = "Screen";

    public async Task<PagedResult<AssetModelListItemDto>> ListAsync(
        AssetModelListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetModels.AsNoTracking().AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (!string.IsNullOrWhiteSpace(query.Manufacturer))
        {
            var manufacturer = query.Manufacturer.Trim();
            source = source.Where(x => x.Manufacturer.Name == manufacturer);
        }
        if (!string.IsNullOrWhiteSpace(query.AssetType))
        {
            var assetType = query.AssetType.Trim();
            source = source.Where(x => x.AssetType != null && x.AssetType.Name == assetType);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) ||
                (x.ModelNumber != null && x.ModelNumber.Contains(search)) ||
                x.Manufacturer.Name.Contains(search) ||
                (x.AssetType != null && x.AssetType.Name.Contains(search)) ||
                (x.AlternateName != null && x.AlternateName.Contains(search)));
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("modelnumber", "desc") => source.OrderByDescending(x => x.ModelNumber),
            ("modelnumber", _) => source.OrderBy(x => x.ModelNumber),
            ("manufacturer", "desc") => source.OrderByDescending(x => x.Manufacturer.Name),
            ("manufacturer", _) => source.OrderBy(x => x.Manufacturer.Name),
            ("active", "desc") => source.OrderByDescending(x => x.IsActive),
            ("active", _) => source.OrderBy(x => x.IsActive),
            (_, "desc") => source.OrderByDescending(x => x.Name),
            _ => source.OrderBy(x => x.Name)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetModelListItemDto(x.Id, x.Name, x.Manufacturer.Name, x.ModelNumber, x.IsActive))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<AssetModelDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await MapDetailAsync(await FindAsync(id, cancellationToken), cancellationToken);

    public async Task<AssetModelDetailDto> CreateAsync(
        AssetModelRequest request, CancellationToken cancellationToken)
    {
        var manufacturer = await ResolveManufacturerAsync(request.Manufacturer, cancellationToken)
            ?? throw new NotFoundException("Manufacturer is required.");
        await EnsureUniqueModelNumberAsync(manufacturer.Id, request.ModelNumber, null, cancellationToken);
        var includeMore = request.MoreInformation == true;
        var assetType = includeMore
            ? await ResolveAssetTypeAsync(request.AssetType, cancellationToken)
            : null;
        var entity = new AssetModel { Code = await AllocateCodeAsync(request.ModelNumber, null, cancellationToken) };
        Apply(entity, request, manufacturer.Id, assetType?.Id, includeMore);
        db.AssetModels.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Name set to '{entity.Name}'");
        AddHistory(entity.Id, $"Manufacturer set to '{manufacturer.Name}'");
        if (!string.IsNullOrWhiteSpace(entity.ModelNumber))
            AddHistory(entity.Id, $"Model Number set to '{entity.ModelNumber}'");
        AddHistory(entity.Id, $"Active set to {YesNoParser.Format(entity.IsActive)}");
        if (includeMore)
            AddMoreInformationHistory(entity, assetType?.Name);
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<AssetModelDetailDto> UpdateAsync(
        Guid id, AssetModelRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        var manufacturer = await ResolveManufacturerAsync(request.Manufacturer, cancellationToken)
            ?? throw new NotFoundException("Manufacturer is required.");
        await EnsureUniqueModelNumberAsync(manufacturer.Id, request.ModelNumber, id, cancellationToken);
        var includeMore = request.MoreInformation == true;
        var assetType = includeMore
            ? await ResolveAssetTypeAsync(request.AssetType, cancellationToken)
            : null;
        var modelNumber = string.IsNullOrWhiteSpace(request.ModelNumber) ? "" : request.ModelNumber.Trim();

        Track(entity.Id, "Name", entity.Name, request.Name.Trim());
        Track(entity.Id, "Manufacturer", entity.Manufacturer.Name, manufacturer.Name);
        Track(entity.Id, "Model Number", entity.ModelNumber, modelNumber);
        Track(entity.Id, "Active", YesNoParser.Format(entity.IsActive),
            YesNoParser.Format(request.Active == true));
        if (includeMore)
        {
            Track(entity.Id, "Alternate Name", entity.AlternateName, NullIfEmpty(request.AlternateName));
            Track(entity.Id, "Asset Type", entity.AssetType?.Name, assetType?.Name);
            Track(entity.Id, "Specifications", entity.Specifications, NullIfEmpty(request.Specifications));
            Track(entity.Id, "Expected Useful Life", entity.ExpectedUsefulLifeMonths?.ToString(),
                request.ExpectedUsefulLife?.ToString());
            Track(entity.Id, "Documentation", entity.Documentation, NullIfEmpty(request.Documentation));
        }

        if (!string.Equals(entity.ModelNumber, modelNumber, StringComparison.Ordinal))
            entity.Code = await AllocateCodeAsync(modelNumber, id, cancellationToken);
        Apply(entity, request, manufacturer.Id, assetType?.Id, includeMore);
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

    public async Task<AssetModelDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
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
        var source = db.AssetModels.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.Name.Contains(search) ||
                (x.ModelNumber != null && x.ModelNumber.Contains(search)) ||
                x.Code.Contains(search));
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssetModelHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.AssetModels.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Asset model was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetModelHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<AssetModel> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetModels.Include(x => x.Manufacturer).Include(x => x.AssetType)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Asset model was not found.");

    private static void Apply(
        AssetModel entity, AssetModelRequest request, Guid manufacturerId, Guid? assetTypeId, bool includeMoreInformation)
    {
        entity.Name = request.Name.Trim();
        entity.ManufacturerId = manufacturerId;
        entity.ModelNumber = string.IsNullOrWhiteSpace(request.ModelNumber) ? "" : request.ModelNumber.Trim();
        entity.IsActive = request.Active == true;
        if (includeMoreInformation)
        {
            entity.AlternateName = NullIfEmpty(request.AlternateName);
            entity.AssetTypeId = assetTypeId;
            entity.Specifications = NullIfEmpty(request.Specifications);
            entity.ExpectedUsefulLifeMonths = request.ExpectedUsefulLife;
            entity.Documentation = NullIfEmpty(request.Documentation);
        }
        if (entity.IsActive)
        {
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
        }
        else
            entity.DeletedAtUtc ??= DateTime.UtcNow;
    }

    private void AddMoreInformationHistory(AssetModel entity, string? assetTypeName)
    {
        if (!string.IsNullOrWhiteSpace(entity.AlternateName))
            AddHistory(entity.Id, $"Alternate Name set to '{entity.AlternateName}'");
        if (!string.IsNullOrWhiteSpace(assetTypeName))
            AddHistory(entity.Id, $"Asset Type set to '{assetTypeName}'");
        if (!string.IsNullOrWhiteSpace(entity.Specifications))
            AddHistory(entity.Id, "Specifications updated");
        if (entity.ExpectedUsefulLifeMonths.HasValue)
            AddHistory(entity.Id, $"Expected Useful Life set to '{entity.ExpectedUsefulLifeMonths}'");
        if (!string.IsNullOrWhiteSpace(entity.Documentation))
            AddHistory(entity.Id, "Documentation updated");
    }

    private async Task EnsureUniqueModelNumberAsync(
        Guid manufacturerId, string? modelNumber, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = modelNumber?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;
        if (await db.AssetModels.AnyAsync(x => x.ManufacturerId == manufacturerId && x.ModelNumber == trimmed &&
            (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
            throw new ConflictException($"Model number '{trimmed}' already exists for this manufacturer.");
    }

    private async Task<string> AllocateCodeAsync(
        string? modelNumber, Guid? excludingId, CancellationToken cancellationToken)
    {
        var baseCode = ToCode(modelNumber);
        var code = baseCode;
        var n = 2;
        while (await db.AssetModels.AnyAsync(
            x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId), cancellationToken))
        {
            var suffix = $"-{n++}";
            code = (baseCode.Length + suffix.Length > 50 ? baseCode[..(50 - suffix.Length)] : baseCode) + suffix;
        }
        return code;
    }

    private static string ToCode(string? modelNumber)
    {
        var builder = new StringBuilder(modelNumber?.Length ?? 0);
        foreach (var ch in (modelNumber ?? "").Trim())
        {
            if (char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-')
                builder.Append(ch);
            else if (builder.Length > 0 && builder[^1] != '-')
                builder.Append('-');
        }
        var code = builder.ToString().Trim('-');
        if (string.IsNullOrEmpty(code) || !char.IsLetterOrDigit(code[0]))
            code = "M" + code;
        return code.Length > 50 ? code[..50] : code;
    }

    private async Task<Manufacturer?> ResolveManufacturerAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        Manufacturer? manufacturer;
        if (Guid.TryParse(trimmed, out var id))
            manufacturer = await db.Manufacturers.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        else
            manufacturer = await db.Manufacturers.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Name == trimmed && x.IsActive, cancellationToken);
        return manufacturer ?? throw new NotFoundException($"Manufacturer '{trimmed}' was not found.");
    }

    private async Task<AssetType?> ResolveAssetTypeAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        AssetType? type;
        if (Guid.TryParse(trimmed, out var id))
            type = await db.AssetTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        else
            type = await db.AssetTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == trimmed && x.IsActive, cancellationToken);
        return type ?? throw new NotFoundException($"Asset type '{trimmed}' was not found.");
    }

    private async Task<AssetModelDetailDto> MapDetailAsync(AssetModel entity, CancellationToken cancellationToken)
    {
        var manufacturerName = entity.Manufacturer?.Name;
        if (manufacturerName is null)
            manufacturerName = await db.Manufacturers.AsNoTracking()
                .Where(x => x.Id == entity.ManufacturerId)
                .Select(x => x.Name)
                .SingleAsync(cancellationToken);

        var typeName = entity.AssetType?.Name;
        if (typeName is null && entity.AssetTypeId.HasValue)
            typeName = await db.AssetTypes.AsNoTracking()
                .Where(x => x.Id == entity.AssetTypeId)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(cancellationToken);

        return new(
            entity.Id,
            entity.Name,
            manufacturerName,
            entity.ModelNumber,
            entity.IsActive,
            entity.AlternateName,
            typeName,
            entity.Specifications,
            entity.ExpectedUsefulLifeMonths,
            entity.Documentation,
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
            ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ConflictException("A record with the same unique value already exists.");
        }
    }
}
