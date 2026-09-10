using System.Text.Json;
using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class CustomAttributeDefinitionService(
    AssetsDbContext db,
    ICurrentUser currentUser) : ICustomAttributeDefinitionService
{
    private const string EntityType = nameof(CustomAttributeDefinition);
    private const string Lifecycle = "Draft → Active → Retired";
    private const string Source = "Screen";

    public async Task<PagedResult<CustomAttributeDefinitionListItemDto>> ListAsync(
        CustomAttributeDefinitionListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetTypeAttributes.AsNoTracking()
            .Include(x => x.AssetType)
            .Include(x => x.CustomAttributeDefinition)
            .AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.CustomAttributeDefinition.IsActive == query.Active.Value);
        source = FilterByAssetType(source, query.AssetType, query.AssetTypeId);
        source = FilterByDataType(source, query.DataType);
        source = FilterByEffectiveClass(source, query.EffectiveClass);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.CustomAttributeDefinition.Code.Contains(search) ||
                x.CustomAttributeDefinition.Name.Contains(search) ||
                x.AssetType.Name.Contains(search) ||
                (x.CustomAttributeDefinition.AlternateName != null &&
                    x.CustomAttributeDefinition.AlternateName.Contains(search)));
        }

        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("assettype", "desc") => source.OrderByDescending(x => x.AssetType.Name),
            ("assettype", _) => source.OrderBy(x => x.AssetType.Name),
            ("code", "desc") => source.OrderByDescending(x => x.CustomAttributeDefinition.Code),
            ("code", _) => source.OrderBy(x => x.CustomAttributeDefinition.Code),
            ("label", "desc") => source.OrderByDescending(x => x.CustomAttributeDefinition.Name),
            ("label", _) => source.OrderBy(x => x.CustomAttributeDefinition.Name),
            ("datatype", "desc") => source.OrderByDescending(x => x.CustomAttributeDefinition.DataType),
            ("datatype", _) => source.OrderBy(x => x.CustomAttributeDefinition.DataType),
            ("effectiveclass", "desc") => source.OrderByDescending(x => x.Requirement),
            ("effectiveclass", _) => source.OrderBy(x => x.Requirement),
            ("displayorder", "desc") => source.OrderByDescending(x => x.DisplayOrder),
            ("displayorder", _) => source.OrderBy(x => x.DisplayOrder),
            (_, "desc") => source.OrderByDescending(x => x.AssetType.Name).ThenByDescending(x => x.DisplayOrder),
            _ => source.OrderBy(x => x.AssetType.Name).ThenBy(x => x.DisplayOrder)
        };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        var items = rows.Select(x => new CustomAttributeDefinitionListItemDto(
            x.CustomAttributeDefinitionId, x.AssetType.Name, x.CustomAttributeDefinition.Code,
            x.CustomAttributeDefinition.Name, FormatDataType(x.CustomAttributeDefinition.DataType),
            x.Requirement.ToString(), x.DisplayOrder)).ToArray();
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(items, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    public async Task<CustomAttributeDefinitionDetailDto> GetAsync(
        Guid id, CancellationToken cancellationToken) =>
        MapDetail(await FindAsync(id, cancellationToken));

    public async Task<CustomAttributeDefinitionDetailDto> CreateAsync(
        CustomAttributeDefinitionRequest request, CancellationToken cancellationToken)
    {
        var assetType = await ResolveAssetTypeAsync(request.AssetType, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, assetType.Id, null, cancellationToken);
        var includeMore = YesNoParser.TryParse(request.MoreInformation) == true;
        var dataType = ParseDataType(request.DataType) ?? CustomAttributeDataType.Text;
        var requirement = ParseClass(request.EffectiveClass);
        var order = request.DisplayOrder is > 0
            ? request.DisplayOrder.Value
            : await NextDisplayOrderAsync(assetType.Id, cancellationToken);
        await EnsureUniqueOrderAsync(assetType.Id, order, null, cancellationToken);

        var entity = new CustomAttributeDefinition();
        var assignment = new AssetTypeAttribute { AssetTypeId = assetType.Id, AssetType = assetType };
        Apply(entity, assignment, request, dataType, requirement, order, includeMore);
        assignment.CustomAttributeDefinition = entity;
        db.AssetTypeAttributes.Add(assignment);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Asset Type set to '{assetType.Name}'");
        AddHistory(entity.Id, $"Code set to '{entity.Code}'");
        AddHistory(entity.Id, $"Label set to '{entity.Name}'");
        AddHistory(entity.Id, $"Data Type set to '{FormatDataType(entity.DataType)}'");
        AddHistory(entity.Id, $"Effective Class set to '{assignment.Requirement}'");
        AddHistory(entity.Id, $"Display Order set to '{assignment.DisplayOrder}'");
        AddHistory(entity.Id, $"Show In List set to {YesNoParser.Format(assignment.ShowInList)}");
        AddHistory(entity.Id, $"Active set to {YesNoParser.Format(entity.IsActive)}");
        if (!string.IsNullOrWhiteSpace(entity.Unit))
            AddHistory(entity.Id, $"Unit set to '{entity.Unit}'");
        if (!string.IsNullOrWhiteSpace(entity.HelpText))
            AddHistory(entity.Id, "Help Text updated");
        if (!string.IsNullOrWhiteSpace(entity.AlternateName))
            AddHistory(entity.Id, $"Alternate Name set to '{entity.AlternateName}'");
        await SaveAsync(cancellationToken);
        return MapDetail(await FindAsync(entity.Id, cancellationToken));
    }

    public async Task<CustomAttributeDefinitionDetailDto> UpdateAsync(
        Guid id, CustomAttributeDefinitionRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        var assignment = Assignment(entity);
        var assetType = await ResolveAssetTypeAsync(request.AssetType, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, assetType.Id, id, cancellationToken);
        var hasValues = await db.AssetTypeAttributes.AnyAsync(
            x => x.CustomAttributeDefinitionId == id && x.HasRecordedValues, cancellationToken);
        AssetDataRules.EnsureAttributeCodeCanChange(hasValues, entity.Code, request.Code.Trim());
        var includeMore = YesNoParser.TryParse(request.MoreInformation) == true;
        var dataType = ParseDataType(request.DataType) ?? entity.DataType;
        var requirement = ParseClass(request.EffectiveClass);
        var order = request.DisplayOrder is > 0 ? request.DisplayOrder.Value : assignment.DisplayOrder;
        await EnsureUniqueOrderAsync(assetType.Id, order, assignment.Id, cancellationToken);

        Track(entity.Id, "Asset Type", assignment.AssetType.Name, assetType.Name);
        Track(entity.Id, "Code", entity.Code, request.Code.Trim());
        Track(entity.Id, "Label", entity.Name, request.Label.Trim());
        Track(entity.Id, "Data Type", FormatDataType(entity.DataType), FormatDataType(dataType));
        Track(entity.Id, "Effective Class", assignment.Requirement.ToString(), requirement.ToString());
        Track(entity.Id, "Display Order", assignment.DisplayOrder.ToString(), order.ToString());
        Track(entity.Id, "Show In List", YesNoParser.Format(assignment.ShowInList),
            YesNoParser.Format(YesNoParser.TryParse(request.ShowInList) == true));
        Track(entity.Id, "Active", YesNoParser.Format(entity.IsActive),
            YesNoParser.Format(YesNoParser.TryParse(request.Active) == true));
        Track(entity.Id, "Unit", entity.Unit, NullIfEmpty(request.Unit));
        Track(entity.Id, "Help Text", entity.HelpText, NullIfEmpty(request.HelpText));
        if (includeMore)
        {
            Track(entity.Id, "Alternate Name", entity.AlternateName, NullIfEmpty(request.AlternateName));
            Track(entity.Id, "List Values", entity.ListValuesJson, SerializeList(request.ListValues, dataType));
            Track(entity.Id, "Alternate Help Text", entity.AlternateHelpText, NullIfEmpty(request.AlternateHelpText));
        }

        assignment.AssetTypeId = assetType.Id;
        assignment.AssetType = assetType;
        Apply(entity, assignment, request, dataType, requirement, order, includeMore);
        await SaveAsync(cancellationToken);
        return MapDetail(await FindAsync(id, cancellationToken));
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive) return;
        AddHistory(entity.Id, "Active changed from 'Yes' to 'No'");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        foreach (var assignment in entity.AssetTypes.Where(x => x.IsActive))
        {
            assignment.IsActive = false;
            assignment.DeletedAtUtc = entity.DeletedAtUtc;
            assignment.DeletedBy = currentUser.UserName;
        }
        await SaveAsync(cancellationToken);
    }

    public async Task<CustomAttributeDefinitionDetailDto> RestoreAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (!entity.IsActive)
        {
            AddHistory(entity.Id, "Active changed from 'No' to 'Yes'");
            entity.IsActive = true;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            foreach (var assignment in entity.AssetTypes)
            {
                assignment.IsActive = true;
                assignment.DeletedAtUtc = null;
                assignment.DeletedBy = null;
            }
            await SaveAsync(cancellationToken);
        }
        return MapDetail(entity);
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(
        string? search, CancellationToken cancellationToken)
    {
        var source = db.CustomAttributeDefinitions.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.Name.Contains(term) || x.Code.Contains(term));
        }
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CustomAttributeDefinitionHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!await db.CustomAttributeDefinitions.AnyAsync(x => x.Id == id, cancellationToken))
            throw new NotFoundException("Custom attribute definition was not found.");
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new CustomAttributeDefinitionHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string code, string? assetType, Guid? excludingId, CancellationToken cancellationToken)
    {
        var source = db.AssetTypeAttributes.Where(x => x.CustomAttributeDefinition.Code == code &&
            (!excludingId.HasValue || x.CustomAttributeDefinitionId != excludingId));
        if (!string.IsNullOrWhiteSpace(assetType))
        {
            var type = assetType.Trim();
            if (Guid.TryParse(type, out var typeId))
                source = source.Where(x => x.AssetTypeId == typeId);
            else
                source = source.Where(x => x.AssetType.Name == type || x.AssetType.Code == type);
        }
        return await source.AnyAsync(cancellationToken);
    }

    private async Task<CustomAttributeDefinition> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.CustomAttributeDefinitions
            .Include(x => x.AssetTypes).ThenInclude(x => x.AssetType)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Custom attribute definition was not found.");

    private static AssetTypeAttribute Assignment(CustomAttributeDefinition entity) =>
        entity.AssetTypes.OrderBy(x => x.DisplayOrder).FirstOrDefault()
        ?? throw new NotFoundException("Custom attribute definition was not found.");

    private async Task<AssetType> ResolveAssetTypeAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new NotFoundException("Asset type was not found.");
        var trimmed = value.Trim();
        AssetType? type;
        if (Guid.TryParse(trimmed, out var id))
            type = await db.AssetTypes.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        else
            type = await db.AssetTypes.SingleOrDefaultAsync(
                x => (x.Name == trimmed || x.Code == trimmed) && x.IsActive, cancellationToken);
        return type ?? throw new NotFoundException($"Asset type '{trimmed}' was not found.");
    }

    private static void Apply(
        CustomAttributeDefinition entity,
        AssetTypeAttribute assignment,
        CustomAttributeDefinitionRequest request,
        CustomAttributeDataType dataType,
        AttributeRequirement requirement,
        int displayOrder,
        bool includeMoreInformation)
    {
        entity.Code = request.Code.Trim();
        entity.Name = request.Label.Trim();
        entity.DataType = dataType;
        entity.Unit = NullIfEmpty(request.Unit);
        entity.HelpText = NullIfEmpty(request.HelpText);
        entity.IsActive = YesNoParser.TryParse(request.Active) == true;
        if (includeMoreInformation)
        {
            entity.AlternateName = NullIfEmpty(request.AlternateName);
            entity.ListValuesJson = SerializeList(request.ListValues, dataType);
            entity.AlternateHelpText = NullIfEmpty(request.AlternateHelpText);
        }
        else if (dataType == CustomAttributeDataType.List)
            entity.ListValuesJson = SerializeList(request.ListValues, dataType);
        if (dataType != CustomAttributeDataType.List)
            entity.ListValuesJson = null;
        assignment.Requirement = requirement;
        assignment.DisplayOrder = displayOrder;
        assignment.ShowInList = YesNoParser.TryParse(request.ShowInList) == true;
        assignment.IsActive = entity.IsActive;
        if (entity.IsActive)
        {
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            assignment.DeletedAtUtc = null;
            assignment.DeletedBy = null;
        }
        else
        {
            entity.DeletedAtUtc ??= DateTime.UtcNow;
            assignment.DeletedAtUtc ??= entity.DeletedAtUtc;
        }
    }

    private async Task EnsureUniqueCodeAsync(
        string code, Guid assetTypeId, Guid? excludingDefinitionId, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();
        if (await db.AssetTypeAttributes.AnyAsync(x => x.AssetTypeId == assetTypeId &&
            x.CustomAttributeDefinition.Code == trimmed &&
            (!excludingDefinitionId.HasValue || x.CustomAttributeDefinitionId != excludingDefinitionId),
            cancellationToken))
            throw new ConflictException("That code already exists on this type.");
    }

    private async Task EnsureUniqueOrderAsync(
        Guid assetTypeId, int displayOrder, Guid? excludingAssignmentId, CancellationToken cancellationToken)
    {
        if (await db.AssetTypeAttributes.AnyAsync(x => x.AssetTypeId == assetTypeId &&
            x.DisplayOrder == displayOrder &&
            (!excludingAssignmentId.HasValue || x.Id != excludingAssignmentId), cancellationToken))
            throw new ConflictException("That display order is already used on this asset type.");
    }

    private async Task<int> NextDisplayOrderAsync(Guid assetTypeId, CancellationToken cancellationToken)
    {
        var max = await db.AssetTypeAttributes
            .Where(x => x.AssetTypeId == assetTypeId)
            .MaxAsync(x => (int?)x.DisplayOrder, cancellationToken);
        return (max ?? 0) + 1;
    }

    private CustomAttributeDefinitionDetailDto MapDetail(CustomAttributeDefinition entity)
    {
        var assignment = Assignment(entity);
        return new(entity.Id, assignment.AssetType.Name, entity.Code, entity.Name,
            FormatDataType(entity.DataType), assignment.Requirement.ToString(), assignment.DisplayOrder,
            YesNoParser.Format(assignment.ShowInList), YesNoParser.Format(entity.IsActive),
            entity.AlternateName, DeserializeList(entity.ListValuesJson), entity.Unit,
            entity.HelpText, entity.AlternateHelpText, Lifecycle);
    }

    private static IQueryable<AssetTypeAttribute> FilterByAssetType(
        IQueryable<AssetTypeAttribute> source, string? assetType, Guid? assetTypeId)
    {
        if (assetTypeId is { } id && id != Guid.Empty)
            source = source.Where(x => x.AssetTypeId == id);
        if (string.IsNullOrWhiteSpace(assetType))
            return source;
        var value = assetType.Trim();
        if (Guid.TryParse(value, out var parsedId))
            return source.Where(x => x.AssetTypeId == parsedId);
        var term = value.ToLower();
        return source.Where(x => x.AssetType.Name.ToLower() == term ||
            x.AssetType.Code.ToLower() == term ||
            x.AssetType.Name.ToLower().Contains(term) ||
            x.AssetType.Code.ToLower().Contains(term) ||
            (x.AssetType.AlternateName != null && x.AssetType.AlternateName.ToLower().Contains(term)));
    }

    private static IQueryable<AssetTypeAttribute> FilterByDataType(
        IQueryable<AssetTypeAttribute> source, string? dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType))
            return source;
        var parsed = ParseDataType(dataType);
        return parsed.HasValue
            ? source.Where(x => x.CustomAttributeDefinition.DataType == parsed.Value)
            : source.Where(x => false);
    }

    private static IQueryable<AssetTypeAttribute> FilterByEffectiveClass(
        IQueryable<AssetTypeAttribute> source, string? effectiveClass)
    {
        if (string.IsNullOrWhiteSpace(effectiveClass))
            return source;
        var parsed = TryParseClass(effectiveClass);
        return parsed.HasValue
            ? source.Where(x => x.Requirement == parsed.Value)
            : source.Where(x => false);
    }

    private static CustomAttributeDataType? ParseDataType(string? value)
    {
        var formatted = CustomAttributeDefinitionRequestValidator.FormatDataType(value);
        return formatted switch
        {
            "Text" => CustomAttributeDataType.Text,
            "Number" => CustomAttributeDataType.Number,
            "Money" => CustomAttributeDataType.Money,
            "Date" => CustomAttributeDataType.Date,
            "Yes or no" => CustomAttributeDataType.YesNo,
            "List" => CustomAttributeDataType.List,
            "Reference" => CustomAttributeDataType.Reference,
            _ when int.TryParse(value?.Trim(), out var number) &&
                   Enum.IsDefined(typeof(CustomAttributeDataType), number) =>
                (CustomAttributeDataType)number,
            _ => null
        };
    }

    private static AttributeRequirement ParseClass(string? value) =>
        TryParseClass(value) ?? AttributeRequirement.Optional;

    private static AttributeRequirement? TryParseClass(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        return Enum.TryParse<AttributeRequirement>(trimmed, true, out var cls) ? cls : null;
    }

    private static string FormatDataType(CustomAttributeDataType value) =>
        value == CustomAttributeDataType.YesNo ? "Yes or no" : value.ToString();

    private static string? SerializeList(IReadOnlyCollection<string>? values, CustomAttributeDataType dataType)
    {
        if (dataType != CustomAttributeDataType.List) return null;
        var cleaned = values?.Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        return cleaned is { Length: > 0 } ? JsonSerializer.Serialize(cleaned) : null;
    }

    private static IReadOnlyCollection<string>? DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<string[]>(json);
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
