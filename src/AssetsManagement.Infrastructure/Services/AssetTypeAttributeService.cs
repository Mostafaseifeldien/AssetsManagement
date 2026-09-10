using System.Text.Json;
using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetTypeAttributeService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IAssetTypeAttributeService
{
    public async Task<PagedResult<TypeAttributeListItemDto>> ListAsync(
        TypeAttributeListQuery query, CancellationToken cancellationToken)
    {
        var source = Filter(query);
        source = Sort(source, query);
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return Page(rows.Select(ToListItem).ToArray(), query.PageNumber, query.PageSize, total);
    }

    public async Task<IReadOnlyCollection<TypeAttributeGroupDto>> ListGroupedAsync(
        TypeAttributeListQuery query, CancellationToken cancellationToken)
    {
        var rows = await Sort(Filter(query), query).ToArrayAsync(cancellationToken);
        var typeIds = rows.Select(x => x.AssetTypeId).Distinct().ToArray();
        var counts = await db.Assets.AsNoTracking()
            .Where(x => typeIds.Contains(x.AssetTypeId) && x.IsActive)
            .GroupBy(x => x.AssetTypeId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return rows
            .GroupBy(x => new { x.AssetTypeId, x.AssetType.Name })
            .Select(g => new TypeAttributeGroupDto(
                g.Key.AssetTypeId,
                g.Key.Name,
                counts.GetValueOrDefault(g.Key.AssetTypeId),
                g.Count(),
                g.Select(ToListItem).ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TypeAttributeTypeOptionDto>> ListTypesAsync(
        CancellationToken cancellationToken)
    {
        var types = await db.AssetTypes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToArrayAsync(cancellationToken);
        var ids = types.Select(x => x.Id).ToArray();
        var counts = await db.Assets.AsNoTracking()
            .Where(x => ids.Contains(x.AssetTypeId) && x.IsActive)
            .GroupBy(x => x.AssetTypeId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        return types.Select(t => new TypeAttributeTypeOptionDto(
            t.Id, t.Name, counts.GetValueOrDefault(t.Id))).ToArray();
    }

    public async Task<TypeAttributeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await MapDetailAsync(await FindAsync(id, cancellationToken, asNoTracking: true), cancellationToken);

    public async Task<TypeAttributeDetailDto> CreateFieldAsync(
        Guid? assetTypeId, TypeAttributeFieldRequest request, CancellationToken cancellationToken)
    {
        var assetType = await ResolveAssetTypeAsync(FirstNonEmpty(request.AssetType, assetTypeId?.ToString()),
            cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, assetType.Id, null, cancellationToken);
        var dataType = ParseDataType(request.DataType) ?? CustomAttributeDataType.Text;
        var requirement = ParseClass(TypeAttributeFieldRequestValidator.ResolveClass(request));
        var order = request.DisplayOrder is int value && value != 0
            ? value
            : await NextDisplayOrderAsync(assetType.Id, cancellationToken);
        await EnsureUniqueOrderAsync(assetType.Id, order, null, cancellationToken);

        var definition = new CustomAttributeDefinition();
        var assignment = new AssetTypeAttribute { AssetTypeId = assetType.Id, AssetType = assetType };
        ApplyField(definition, assignment, request, dataType, requirement, order);
        assignment.CustomAttributeDefinition = definition;
        db.AssetTypeAttributes.Add(assignment);
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(
            await FindAsync(assignment.Id, cancellationToken, asNoTracking: true), cancellationToken);
    }

    public async Task<TypeAttributeDetailDto> UpdateFieldAsync(
        Guid id, TypeAttributeFieldRequest request, CancellationToken cancellationToken)
    {
        var assignment = await FindAsync(id, cancellationToken, asNoTracking: false);
        var definition = assignment.CustomAttributeDefinition;
        var assetType = string.IsNullOrWhiteSpace(request.AssetType)
            ? assignment.AssetType
            : await ResolveAssetTypeAsync(request.AssetType, cancellationToken);
        await EnsureUniqueCodeAsync(request.Code, assetType.Id, definition.Id, cancellationToken);
        var hasValues = assignment.HasRecordedValues;
        AssetDataRules.EnsureAttributeCodeCanChange(hasValues, definition.Code, request.Code.Trim());
        var dataType = ParseDataType(request.DataType) ?? definition.DataType;
        var requirement = ParseClass(TypeAttributeFieldRequestValidator.ResolveClass(request));
        var order = request.DisplayOrder ?? assignment.DisplayOrder;
        await EnsureUniqueOrderAsync(assetType.Id, order, assignment.Id, cancellationToken);

        assignment.AssetTypeId = assetType.Id;
        assignment.AssetType = assetType;
        ApplyField(definition, assignment, request, dataType, requirement, order);
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(await FindAsync(id, cancellationToken, asNoTracking: true), cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await FindAsync(id, cancellationToken, asNoTracking: false);
        if (!assignment.IsActive) return;
        var now = DateTime.UtcNow;
        assignment.IsActive = false;
        assignment.DeletedAtUtc = now;
        assignment.DeletedBy = currentUser.UserName;
        var definition = assignment.CustomAttributeDefinition;
        definition.IsActive = false;
        definition.DeletedAtUtc = now;
        definition.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<TypeAttributeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await FindAsync(id, cancellationToken, asNoTracking: false);
        if (!assignment.IsActive)
        {
            assignment.IsActive = true;
            assignment.DeletedAtUtc = null;
            assignment.DeletedBy = null;
            var definition = assignment.CustomAttributeDefinition;
            definition.IsActive = true;
            definition.DeletedAtUtc = null;
            definition.DeletedBy = null;
            await SaveAsync(cancellationToken);
        }
        return await MapDetailAsync(await FindAsync(id, cancellationToken, asNoTracking: true), cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string code, string? assetType, Guid? excludingId, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();
        var source = db.AssetTypeAttributes.Where(x => x.CustomAttributeDefinition.Code == trimmed);
        if (!string.IsNullOrWhiteSpace(assetType))
        {
            var type = assetType.Trim();
            source = Guid.TryParse(type, out var typeId)
                ? source.Where(x => x.AssetTypeId == typeId)
                : source.Where(x => x.AssetType.Name == type || x.AssetType.Code == type);
        }
        return await source.AnyAsync(x => !excludingId.HasValue || x.Id != excludingId, cancellationToken);
    }

    private IQueryable<AssetTypeAttribute> Filter(TypeAttributeListQuery query)
    {
        var source = db.AssetTypeAttributes.AsNoTracking()
            .Include(x => x.AssetType)
            .Include(x => x.CustomAttributeDefinition)
            .AsQueryable();
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (query.ShowInList.HasValue)
            source = source.Where(x => x.ShowInList == query.ShowInList.Value);
        if (query.AssetTypeId.HasValue)
            source = source.Where(x => x.AssetTypeId == query.AssetTypeId);
        else if (!string.IsNullOrWhiteSpace(query.AssetType))
        {
            var type = query.AssetType.Trim();
            source = Guid.TryParse(type, out var typeId)
                ? source.Where(x => x.AssetTypeId == typeId)
                : source.Where(x => x.AssetType.Name == type || x.AssetType.Code == type);
        }
        if (!string.IsNullOrWhiteSpace(query.DataType))
        {
            var parsed = ParseDataType(query.DataType);
            if (parsed.HasValue)
                source = source.Where(x => x.CustomAttributeDefinition.DataType == parsed.Value);
        }
        var cls = FirstNonEmpty(query.Class, query.Requirement);
        if (!string.IsNullOrWhiteSpace(cls) &&
            Enum.TryParse<AttributeRequirement>(cls, true, out var requirement))
            source = source.Where(x => x.Requirement == requirement);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.CustomAttributeDefinition.Code.Contains(search) ||
                x.CustomAttributeDefinition.Name.Contains(search) ||
                x.AssetType.Name.Contains(search) ||
                (x.CustomAttributeDefinition.AlternateName != null &&
                    x.CustomAttributeDefinition.AlternateName.Contains(search)) ||
                (x.CustomAttributeDefinition.HelpText != null &&
                    x.CustomAttributeDefinition.HelpText.Contains(search)));
        }
        return source;
    }

    private static IQueryable<AssetTypeAttribute> Sort(
        IQueryable<AssetTypeAttribute> source, TypeAttributeListQuery query) =>
        (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("assettype", "desc") => source.OrderByDescending(x => x.AssetType.Name),
            ("assettype", _) => source.OrderBy(x => x.AssetType.Name),
            ("label", "desc") => source.OrderByDescending(x => x.CustomAttributeDefinition.Name),
            ("label", _) => source.OrderBy(x => x.CustomAttributeDefinition.Name),
            ("code", "desc") => source.OrderByDescending(x => x.CustomAttributeDefinition.Code),
            ("code", _) => source.OrderBy(x => x.CustomAttributeDefinition.Code),
            ("datatype", "desc") => source.OrderByDescending(x => x.CustomAttributeDefinition.DataType),
            ("datatype", _) => source.OrderBy(x => x.CustomAttributeDefinition.DataType),
            ("class", "desc") or ("requirement", "desc") => source.OrderByDescending(x => x.Requirement),
            ("class", _) or ("requirement", _) => source.OrderBy(x => x.Requirement),
            ("displayorder", "desc") => source.OrderByDescending(x => x.DisplayOrder),
            ("displayorder", _) => source.OrderBy(x => x.DisplayOrder),
            (_, "desc") => source.OrderByDescending(x => x.AssetType.Name).ThenByDescending(x => x.DisplayOrder),
            _ => source.OrderBy(x => x.AssetType.Name).ThenBy(x => x.DisplayOrder)
        };

    private async Task<AssetTypeAttribute> FindAsync(
        Guid id, CancellationToken cancellationToken, bool asNoTracking)
    {
        var source = db.AssetTypeAttributes
            .Include(x => x.AssetType)
            .Include(x => x.CustomAttributeDefinition)
            .AsQueryable();
        if (asNoTracking) source = source.AsNoTracking();
        return await source.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Type attribute was not found.");
    }

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

    private static void ApplyField(
        CustomAttributeDefinition definition,
        AssetTypeAttribute assignment,
        TypeAttributeFieldRequest request,
        CustomAttributeDataType dataType,
        AttributeRequirement requirement,
        int displayOrder)
    {
        definition.Code = request.Code.Trim();
        definition.Name = request.Label.Trim();
        definition.AlternateName = NullIfEmpty(request.AlternateName);
        definition.DataType = dataType;
        definition.Unit = dataType is CustomAttributeDataType.Number or CustomAttributeDataType.Money
            ? NullIfEmpty(request.Unit) : null;
        definition.HelpText = NullIfEmpty(request.HelpText);
        definition.ListValuesJson = SerializeList(request.PossibleValues, dataType);
        assignment.Requirement = requirement;
        assignment.DisplayOrder = displayOrder;
        assignment.ShowInList = request.ShowInList == true;
    }

    private static TypeAttributeListItemDto ToListItem(AssetTypeAttribute x) =>
        new(x.Id, x.AssetTypeId, x.AssetType.Name, x.CustomAttributeDefinition.Name,
            x.CustomAttributeDefinition.AlternateName, x.CustomAttributeDefinition.Code,
            FormatDataType(x.CustomAttributeDefinition.DataType),
            x.CustomAttributeDefinition.DataType == CustomAttributeDataType.List
                ? DeserializeList(x.CustomAttributeDefinition.ListValuesJson) : null,
            x.CustomAttributeDefinition.Unit, x.Requirement.ToString(), x.ShowInList,
            x.CustomAttributeDefinition.HelpText, x.IsActive);

    private async Task<TypeAttributeDetailDto> MapDetailAsync(
        AssetTypeAttribute x, CancellationToken cancellationToken)
    {
        var assetCount = await db.Assets.CountAsync(
            a => a.AssetTypeId == x.AssetTypeId && a.IsActive, cancellationToken);
        return new(x.Id, x.AssetTypeId, x.AssetType.Name, x.CustomAttributeDefinitionId,
            x.CustomAttributeDefinition.Name, x.CustomAttributeDefinition.AlternateName,
            x.CustomAttributeDefinition.Code, x.HasRecordedValues,
            FormatDataType(x.CustomAttributeDefinition.DataType),
            x.CustomAttributeDefinition.DataType == CustomAttributeDataType.List
                ? DeserializeList(x.CustomAttributeDefinition.ListValuesJson) : null,
            x.CustomAttributeDefinition.Unit, x.Requirement.ToString(), x.DisplayOrder, x.ShowInList,
            x.CustomAttributeDefinition.HelpText, x.IsActive, assetCount);
    }

    private static CustomAttributeDataType? ParseDataType(string? value)
    {
        if (!TypeAttributeFieldRequestValidator.IsDataType(value)) return null;
        var formatted = CustomAttributeDefinitionRequestValidator.FormatDataType(value);
        return formatted == "Yes or no"
            ? CustomAttributeDataType.YesNo
            : Enum.TryParse<CustomAttributeDataType>(formatted, true, out var type) ? type : null;
    }

    private static AttributeRequirement ParseClass(string? value) =>
        Enum.TryParse<AttributeRequirement>(value, true, out var cls) ? cls : AttributeRequirement.Optional;

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

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, int pageNumber, int pageSize, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new(rows, pageNumber, pageSize, total, pages, pageNumber > 1, pageNumber < pages);
    }

    private static string? FirstNonEmpty(string? value, string? fallback) =>
        !string.IsNullOrWhiteSpace(value) ? value : fallback;

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
