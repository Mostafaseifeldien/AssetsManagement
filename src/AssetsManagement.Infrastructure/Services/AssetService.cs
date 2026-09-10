using System.Text.Json;
using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetService(AssetsDbContext db, ICurrentUser currentUser) : IAssetService
{
    private const string EntityType = nameof(Asset);
    private const string Lifecycle = "Draft → Active → In Maintenance → Suspended → Disposed → Archived";
    private const string Source = "Screen";

    public async Task<PagedResult<AssetListItemDto>> ListAsync(AssetListQuery query, CancellationToken cancellationToken)
    {
        var source = Filter(db.Assets.AsNoTracking(), query);
        source = Sort(source, query.SortBy, query.SortDirection);
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetListItemDto(
                x.Id, x.Name, x.AssetNumber, x.AssetTypeId, x.AssetStatusId, x.AssetCategoryId,
                x.CurrentCustodianId, x.OwningDepartment, x.CurrentLocation, x.Criticality,
                x.PurchaseValue, x.IsActive))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query.PageNumber, query.PageSize, total);
    }

    public async Task<MissingAssetsResult> ListMissingAsync(MissingAssetQuery query, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-query.MissingAfterMinutes);
        var missing = db.Assets.AsNoTracking().Where(x => x.IsActive &&
            (x.LastSeenAtUtc == null || x.LastSeenAtUtc < cutoff));
        missing = Filter(missing, query);
        if (query.Silence is "Over a day")
            missing = missing.Where(x => x.LastSeenAtUtc == null || x.LastSeenAtUtc < DateTime.UtcNow.AddDays(-1));
        else if (query.Silence is "Over an hour")
            missing = missing.Where(x => x.LastSeenAtUtc == null || x.LastSeenAtUtc < DateTime.UtcNow.AddHours(-1));
        else if (query.Silence is "Under an hour")
            missing = missing.Where(x => x.LastSeenAtUtc != null && x.LastSeenAtUtc >= DateTime.UtcNow.AddHours(-1));

        var totalAssets = await db.Assets.CountAsync(x => x.IsActive, cancellationToken);
        var notReporting = await missing.CountAsync(cancellationToken);
        var silentOverADay = await missing.CountAsync(x =>
            x.LastSeenAtUtc == null || x.LastSeenAtUtc < DateTime.UtcNow.AddDays(-1), cancellationToken);
        var highCriticality = await missing.CountAsync(x => x.Criticality == "High", cancellationToken);
        var sorted = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("lastseen", "asc") => missing.OrderBy(x => x.LastSeenAtUtc),
            (_, "asc") => missing.OrderBy(x => x.LastSeenAtUtc ?? DateTime.MinValue),
            _ => missing.OrderBy(x => x.LastSeenAtUtc)
        };
        if (string.IsNullOrWhiteSpace(query.SortBy) || query.SortBy.Equals("silentfor", StringComparison.OrdinalIgnoreCase))
            sorted = missing.OrderBy(x => x.LastSeenAtUtc);
        var total = await sorted.CountAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var rows = await sorted.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new MissingAssetListItemDto(
                x.Id, x.Name, x.AssetNumber, x.AssetTypeId, x.CurrentCustodianId, x.OwningDepartment,
                x.CurrentLocation, x.LastSeenAtUtc,
                x.LastSeenAtUtc == null ? int.MaxValue : (int)(now - x.LastSeenAtUtc.Value).TotalMinutes,
                x.Criticality, x.PurchaseValue))
            .ToArrayAsync(cancellationToken);
        var percent = totalAssets == 0 ? 0 : (int)Math.Round(notReporting / (double)totalAssets * 100);
        return new(notReporting, silentOverADay, highCriticality, totalAssets, percent,
            Page(rows, query.PageNumber, query.PageSize, total));
    }

    public async Task<AssetDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await MapDetailAsync(await FindAsync(id, cancellationToken), cancellationToken);

    public async Task<AssetDetailDto> CreateAsync(AssetRequest request, CancellationToken cancellationToken)
    {
        var type = await RequireTypeAsync(request.AssetTypeId, cancellationToken);
        var status = await ResolveStatusAsync(request.AssetStatusId, type, cancellationToken);
        await EnsureModelAsync(request.AssetModelId, type.Id, cancellationToken);
        await EnsureParentAsync(request.ParentAssetId, null, cancellationToken);
        var number = await NextNumberAsync(type, request.AssetNumber, cancellationToken);
        await EnsureUniqueNumberAsync(number, null, cancellationToken);
        await EnsureSerialAsync(type, request, null, cancellationToken);
        var attrs = await ValidateCustomAsync(type.Id, request.CustomAttributes, cancellationToken);
        var entity = new Asset();
        Apply(entity, request, type, status.Id, number, attrs);
        entity.Code = number.Length <= 50 ? number : number[..50];
        db.Assets.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Name set to '{entity.Name}'");
        AddHistory(entity.Id, $"Asset Number set to '{entity.AssetNumber}'");
        AddHistory(entity.Id, $"Asset Type set to '{type.Name}'");
        AddHistory(entity.Id, $"Status set to '{status.Name}'");
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<AssetDetailDto> UpdateAsync(Guid id, AssetRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        var type = await RequireTypeAsync(request.AssetTypeId, cancellationToken);
        var status = await ResolveStatusAsync(request.AssetStatusId, type, cancellationToken);
        await EnsureModelAsync(request.AssetModelId, type.Id, cancellationToken);
        await EnsureParentAsync(request.ParentAssetId, id, cancellationToken);
        if (!string.Equals(entity.AssetNumber, request.AssetNumber?.Trim(), StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(type.NumberingFormat) && type.NumberingFormat.Contains('#'))
            throw new DomainRuleException("The numbering scheme is automatic; the asset number cannot be overwritten.");
        var number = string.IsNullOrWhiteSpace(request.AssetNumber) ? entity.AssetNumber : request.AssetNumber.Trim();
        await EnsureUniqueNumberAsync(number, id, cancellationToken);
        await EnsureSerialAsync(type, request, id, cancellationToken);
        if (entity.AssetStatusId != status.Id)
            AssetDataRules.EnsureAssetCanChangeStatus(entity.AssetStatus.IsTerminal, !string.IsNullOrWhiteSpace(request.DisposalReason));
        var attrs = await ValidateCustomAsync(type.Id, request.CustomAttributes, cancellationToken);
        Track(entity.Id, "Name", entity.Name, request.Name.Trim());
        Track(entity.Id, "Asset Number", entity.AssetNumber, number);
        Track(entity.Id, "Status", entity.AssetStatusId.ToString(), status.Id.ToString());
        Track(entity.Id, "Current Location", entity.CurrentLocation, NullIfEmpty(request.CurrentLocation));
        Apply(entity, request, type, status.Id, number, attrs);
        entity.Code = number.Length <= 50 ? number : number[..50];
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<AssetDetailDto> ChangeStatusAsync(
        Guid id, AssetStatusChangeRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        var next = await db.AssetStatuses.SingleOrDefaultAsync(x => x.Id == request.AssetStatusId, cancellationToken)
            ?? throw new NotFoundException("Asset status was not found.");
        if (entity.AssetStatusId == next.Id)
            return await MapDetailAsync(entity, cancellationToken);
        AssetDataRules.EnsureAssetCanChangeStatus(entity.AssetStatus.IsTerminal, !string.IsNullOrWhiteSpace(request.Reason));
        await EnsureTransitionAllowedAsync(entity, next, cancellationToken);
        Track(entity.Id, "Status", entity.AssetStatus.Name, next.Name);
        if (!string.IsNullOrWhiteSpace(request.Reason))
            AddHistory(entity.Id, $"Reinstatement reason set to '{request.Reason.Trim()}'");
        entity.AssetStatusId = next.Id;
        if (next.IsTerminal)
            entity.DisposalDate ??= DateTime.UtcNow.Date;
        await SaveAsync(cancellationToken);
        return await MapDetailAsync(entity, cancellationToken);
    }

    public async Task<AssetDetailDto> UpdateLocationAsync(
        Guid id, AssetLocationRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        Track(entity.Id, "Current Location", entity.CurrentLocation, request.CurrentLocation.Trim());
        Track(entity.Id, "Location Source", entity.LocationSource, request.LocationSource);
        entity.CurrentLocation = request.CurrentLocation.Trim();
        entity.LocationSource = request.LocationSource;
        entity.LocationUpdatedAtUtc = DateTime.UtcNow;
        entity.LastSeenAtUtc = DateTime.UtcNow;
        entity.LastSeenReader = NullIfEmpty(request.LastSeenReader);
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

    public async Task<AssetDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
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

    public async Task<IReadOnlyCollection<AssetLookupDto>> LookupAsync(string? search, CancellationToken cancellationToken)
    {
        var source = db.Assets.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.Name.Contains(term) || x.AssetNumber.Contains(term) || x.Code.Contains(term));
        }
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new AssetLookupDto(x.Id, x.AssetNumber, x.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssetHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await FindAsync(id, cancellationToken);
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    private static IQueryable<Asset> Filter(IQueryable<Asset> source, AssetListQuery query)
    {
        if (query.Active.HasValue)
            source = source.Where(x => x.IsActive == query.Active.Value);
        if (query.AssetTypeId.HasValue)
            source = source.Where(x => x.AssetTypeId == query.AssetTypeId);
        if (query.AssetCategoryId.HasValue)
            source = source.Where(x => x.AssetCategoryId == query.AssetCategoryId);
        if (query.AssetStatusId.HasValue)
            source = source.Where(x => x.AssetStatusId == query.AssetStatusId);
        if (query.AssetModelId.HasValue)
            source = source.Where(x => x.AssetModelId == query.AssetModelId);
        if (query.ManufacturerId.HasValue)
            source = source.Where(x => x.ManufacturerId == query.ManufacturerId);
        if (query.SupplierId.HasValue)
            source = source.Where(x => x.SupplierId == query.SupplierId);
        if (query.CurrentCustodianId.HasValue)
            source = source.Where(x => x.CurrentCustodianId == query.CurrentCustodianId);
        if (!string.IsNullOrWhiteSpace(query.OwningDepartment))
            source = source.Where(x => x.OwningDepartment == query.OwningDepartment.Trim());
        if (!string.IsNullOrWhiteSpace(query.Criticality))
            source = source.Where(x => x.Criticality == query.Criticality.Trim());
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Name.Contains(search) || x.AssetNumber.Contains(search) ||
                (x.SerialNumber != null && x.SerialNumber.Contains(search)) ||
                (x.CurrentLocation != null && x.CurrentLocation.Contains(search)));
        }
        return source;
    }

    private static IQueryable<Asset> Sort(IQueryable<Asset> source, string? sortBy, string direction) =>
        (sortBy?.ToLowerInvariant(), direction) switch
        {
            ("assetnumber", "desc") => source.OrderByDescending(x => x.AssetNumber),
            ("assetnumber", _) => source.OrderBy(x => x.AssetNumber),
            ("assetstatusid", "desc") => source.OrderByDescending(x => x.AssetStatusId),
            ("assetstatusid", _) => source.OrderBy(x => x.AssetStatusId),
            ("assettypeid", "desc") => source.OrderByDescending(x => x.AssetTypeId),
            ("assettypeid", _) => source.OrderBy(x => x.AssetTypeId),
            ("criticality", "desc") => source.OrderByDescending(x => x.Criticality),
            ("criticality", _) => source.OrderBy(x => x.Criticality),
            ("active", "desc") => source.OrderByDescending(x => x.IsActive),
            ("active", _) => source.OrderBy(x => x.IsActive),
            (_, "desc") => source.OrderByDescending(x => x.Name),
            _ => source.OrderBy(x => x.Name)
        };

    private static void Apply(
        Asset entity, AssetRequest request, AssetType type, Guid statusId, string number, string? attrs)
    {
        entity.Name = request.Name.Trim();
        entity.AlternateName = NullIfEmpty(request.AlternateName);
        entity.AssetNumber = number;
        entity.AssetTypeId = type.Id;
        entity.AssetStatusId = statusId;
        entity.AssetCategoryId = request.AssetCategoryId ?? type.AssetCategoryId;
        entity.AssetModelId = request.AssetModelId;
        entity.ManufacturerId = request.ManufacturerId;
        entity.SupplierId = request.SupplierId;
        entity.SerialNumber = NullIfEmpty(request.SerialNumber);
        entity.OwningOrganization = NullIfEmpty(request.OwningOrganization);
        entity.OwningDepartment = NullIfEmpty(request.OwningDepartment);
        entity.CostCenter = NullIfEmpty(request.CostCenter);
        entity.CurrentCustodianId = request.CurrentCustodianId;
        entity.CustodianType = NullIfEmpty(request.CustodianType) ??
            (request.CurrentCustodianId.HasValue ? CustodyTypes.Employee : null);
        entity.CurrentLocation = NullIfEmpty(request.CurrentLocation);
        entity.LocationSource = NullIfEmpty(request.LocationSource);
        entity.PurchaseDate = request.PurchaseDate;
        entity.PurchaseValue = request.PurchaseValue;
        entity.PurchaseReference = NullIfEmpty(request.PurchaseReference);
        entity.WarrantyExpiry = request.WarrantyExpiry;
        entity.DepreciationMethod = NullIfEmpty(request.DepreciationMethod);
        entity.UsefulLifeMonths = request.UsefulLifeMonths;
        entity.ResidualValue = request.ResidualValue;
        entity.Criticality = NullIfEmpty(request.Criticality);
        entity.ParentAssetId = request.ParentAssetId;
        entity.CommissionedDate = request.CommissionedDate;
        entity.DisposalDate = request.DisposalDate;
        entity.DisposalReason = NullIfEmpty(request.DisposalReason);
        entity.CustomAttributesJson = attrs;
        if (request.Active.HasValue)
            entity.IsActive = request.Active.Value;
    }

    private async Task<AssetDetailDto> MapDetailAsync(Asset entity, CancellationToken cancellationToken)
    {
        var rfid = await db.RfidTags.AsNoTracking()
            .Where(x => x.AssetId == entity.Id && x.Status == IdentifierStatus.Assigned)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        var barcode = await db.Barcodes.AsNoTracking()
            .Where(x => x.AssetId == entity.Id && x.Status == IdentifierStatus.Assigned)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        var image = await db.AssetImages.AsNoTracking()
            .Where(x => x.AssetId == entity.Id && x.IsPrimary && x.IsActive)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        return new(
            entity.Id, entity.Name, entity.AlternateName, entity.AssetNumber, entity.AssetTypeId, entity.AssetStatusId,
            entity.AssetCategoryId, entity.AssetModelId, entity.ManufacturerId, entity.SupplierId, entity.SerialNumber,
            entity.OwningOrganization, entity.OwningDepartment, entity.CostCenter, entity.CurrentCustodianId,
            entity.CustodianType, entity.CurrentLocation, entity.LocationUpdatedAtUtc, entity.LocationSource,
            entity.LastSeenAtUtc, entity.LastSeenReader, entity.PurchaseDate, entity.PurchaseValue,
            entity.PurchaseReference, entity.WarrantyExpiry, entity.DepreciationMethod, entity.UsefulLifeMonths,
            entity.ResidualValue, entity.Criticality, entity.ParentAssetId, entity.CommissionedDate, entity.DisposalDate,
            entity.DisposalReason, ParseAttrs(entity.CustomAttributesJson), rfid, barcode, image, entity.IsActive, Lifecycle);
    }

    private async Task<Asset> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Assets.Include(x => x.AssetStatus).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Asset was not found.");

    private async Task<AssetType> RequireTypeAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty)
            throw new DomainRuleException("Asset type id is required.");
        return await db.AssetTypes.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset type was not found.");
    }

    private async Task<AssetStatus> ResolveStatusAsync(Guid? id, AssetType type, CancellationToken cancellationToken)
    {
        var statusId = !id.HasValue || id == Guid.Empty ? type.DefaultStatusId : id;
        if (!statusId.HasValue || statusId == Guid.Empty)
            throw new DomainRuleException("Status id is required.");
        return await db.AssetStatuses.SingleOrDefaultAsync(x => x.Id == statusId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset status was not found.");
    }

    private async Task EnsureModelAsync(Guid? modelId, Guid typeId, CancellationToken cancellationToken)
    {
        if (!modelId.HasValue) return;
        var model = await db.AssetModels.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == modelId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset model was not found.");
        if (model.AssetTypeId.HasValue && model.AssetTypeId != typeId)
            throw new DomainRuleException("The asset model does not belong to the selected asset type.");
    }

    private async Task EnsureParentAsync(Guid? parentId, Guid? selfId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue) return;
        if (selfId.HasValue && parentId == selfId)
            throw new DomainRuleException("An asset cannot be its own parent.");
        var parent = await db.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == parentId, cancellationToken)
            ?? throw new NotFoundException("Parent asset was not found.");
        var seen = new HashSet<Guid>();
        Guid? cursor = parent.ParentAssetId;
        while (cursor.HasValue && seen.Add(cursor.Value))
        {
            if (selfId.HasValue && cursor == selfId)
                throw new DomainRuleException("A Contains relationship cannot create a cycle.");
            cursor = await db.Assets.AsNoTracking().Where(x => x.Id == cursor)
                .Select(x => x.ParentAssetId).SingleOrDefaultAsync(cancellationToken);
        }
    }

    private async Task EnsureSerialAsync(
        AssetType type, AssetRequest request, Guid? excludingId, CancellationToken cancellationToken)
    {
        var serial = NullIfEmpty(request.SerialNumber);
        if (type.RequiresSerialNumber && serial is null)
            throw new DomainRuleException("Serial number is required for this asset type.");
        if (serial is null || !request.ManufacturerId.HasValue) return;
        var exists = await db.Assets.AnyAsync(x =>
            x.ManufacturerId == request.ManufacturerId && x.SerialNumber == serial &&
            (!excludingId.HasValue || x.Id != excludingId), cancellationToken);
        if (exists)
            throw new ConflictException("Serial number must be unique for this manufacturer.");
    }

    private async Task EnsureUniqueNumberAsync(string number, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = await db.Assets.AnyAsync(x => x.AssetNumber == number &&
            (!excludingId.HasValue || x.Id != excludingId), cancellationToken);
        if (exists)
            throw new ConflictException("Asset number is already in use.");
    }

    private async Task<string> NextNumberAsync(AssetType type, string? requested, CancellationToken cancellationToken)
    {
        var format = type.NumberingFormat ?? "";
        var automatic = format.Contains('#');
        if (!automatic)
        {
            var manual = requested?.Trim();
            if (string.IsNullOrWhiteSpace(manual))
                throw new DomainRuleException("Asset number is required when the type has no automatic numbering scheme.");
            return manual;
        }
        if (!string.IsNullOrWhiteSpace(requested))
            throw new DomainRuleException("The numbering scheme is automatic; the asset number cannot be overwritten.");
        var width = format.Count(c => c == '#');
        var prefix = format.TrimEnd('#');
        var existing = await db.Assets.AsNoTracking()
            .Where(x => x.AssetNumber.StartsWith(prefix))
            .Select(x => x.AssetNumber).ToArrayAsync(cancellationToken);
        var max = 0;
        foreach (var value in existing)
        {
            var suffix = value.Length > prefix.Length ? value[prefix.Length..] : "";
            if (int.TryParse(suffix, out var n) && n > max) max = n;
        }
        return prefix + (max + 1).ToString($"D{Math.Max(width, 1)}");
    }

    private async Task EnsureTransitionAllowedAsync(Asset entity, AssetStatus next, CancellationToken cancellationToken)
    {
        var allowed = await db.AssetStatusTransitions.AnyAsync(x =>
            x.FromStatusId == entity.AssetStatusId && x.ToStatusId == next.Id, cancellationToken);
        var any = await db.AssetStatusTransitions.AnyAsync(x => x.FromStatusId == entity.AssetStatusId, cancellationToken);
        if (any && !allowed)
            throw new DomainRuleException("That status transition is not permitted for this asset.");
        var permitted = entity.AssetType?.PermittedStatusTransitions;
        if (permitted is null)
            permitted = await db.AssetTypes.AsNoTracking().Where(x => x.Id == entity.AssetTypeId)
                .Select(x => x.PermittedStatusTransitions).SingleOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(permitted) &&
            !permitted.Contains(next.Name, StringComparison.OrdinalIgnoreCase) &&
            !permitted.Contains(next.Code, StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("That status is not permitted for this asset type.");
    }

    private async Task<string?> ValidateCustomAsync(
        Guid typeId, Dictionary<string, string?>? values, CancellationToken cancellationToken)
    {
        var defs = await db.AssetTypeAttributes.AsNoTracking()
            .Where(x => x.AssetTypeId == typeId && x.IsActive)
            .Include(x => x.CustomAttributeDefinition)
            .ToArrayAsync(cancellationToken);
        var map = values ?? [];
        foreach (var def in defs.Where(x => x.Requirement == AttributeRequirement.Required))
        {
            var value = ReadAttributeValue(map, def.CustomAttributeDefinition);
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainRuleException(
                    $"'{def.CustomAttributeDefinition.Name}' (code '{def.CustomAttributeDefinition.Code}') is required for this asset type.");
        }
        foreach (var pair in map.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
        {
            var match = defs.FirstOrDefault(x =>
                AttributeKeyEquals(pair.Key, x.CustomAttributeDefinition));
            if (match is not null)
            {
                var tracked = await db.AssetTypeAttributes.SingleAsync(x => x.Id == match.Id, cancellationToken);
                tracked.HasRecordedValues = true;
            }
        }
        return map.Count == 0 ? null : JsonSerializer.Serialize(map);
    }

    private static string? ReadAttributeValue(
        Dictionary<string, string?> map, CustomAttributeDefinition definition)
    {
        foreach (var pair in map)
        {
            if (AttributeKeyEquals(pair.Key, definition) && !string.IsNullOrWhiteSpace(pair.Value))
                return pair.Value;
        }
        return null;
    }

    private static bool AttributeKeyEquals(string? key, CustomAttributeDefinition definition) =>
        !string.IsNullOrWhiteSpace(key) &&
        (string.Equals(key, definition.Code, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(key, definition.Name, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyDictionary<string, string?> ParseAttrs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string?>();
        return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new Dictionary<string, string?>();
    }

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, int page, int size, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)size);
        return new(rows, page, size, total, pages, page > 1, page < pages);
    }

    private async Task SaveAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken);

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

    private void AddHistory(Guid entityId, string change) =>
        db.ChangeHistory.Add(new ChangeHistoryEntry
        {
            EntityType = EntityType, EntityId = entityId, WhenUtc = DateTime.UtcNow,
            Change = change, By = currentUser.DisplayName, Source = Source
        });

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
