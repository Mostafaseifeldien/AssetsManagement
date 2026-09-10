using System.Text.Json;
using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetDataService(
    AssetsDbContext db,
    ICurrentUser currentUser) : IAssetDataService
{
    public async Task<PagedResult<AssetDataDto>> ListAsync(
        AssetDataResource resource, ListQuery query, CancellationToken cancellationToken)
    {
        var source = MasterQuery(resource).AsNoTracking();
        source = resource switch
        {
            AssetDataResource.AssetTypes when query.AssetCategoryId.HasValue =>
                db.AssetTypes.AsNoTracking().Where(x => x.AssetCategoryId == query.AssetCategoryId),
            AssetDataResource.AssetCategories when query.ParentId.HasValue =>
                db.AssetCategories.AsNoTracking().Where(x => x.ParentId == query.ParentId),
            AssetDataResource.AssetModels when query.ManufacturerId.HasValue && query.AssetTypeId.HasValue =>
                db.AssetModels.AsNoTracking().Where(x => x.ManufacturerId == query.ManufacturerId &&
                    x.AssetTypeId == query.AssetTypeId),
            AssetDataResource.AssetModels when query.ManufacturerId.HasValue =>
                db.AssetModels.AsNoTracking().Where(x => x.ManufacturerId == query.ManufacturerId),
            AssetDataResource.AssetModels when query.AssetTypeId.HasValue =>
                db.AssetModels.AsNoTracking().Where(x => x.AssetTypeId == query.AssetTypeId),
            AssetDataResource.Suppliers when !string.IsNullOrWhiteSpace(query.SupplierKind) &&
                !string.IsNullOrWhiteSpace(query.Rating) =>
                db.Suppliers.AsNoTracking().Where(x => x.SupplierKind == query.SupplierKind && x.Rating == query.Rating),
            AssetDataResource.Suppliers when !string.IsNullOrWhiteSpace(query.SupplierKind) =>
                db.Suppliers.AsNoTracking().Where(x => x.SupplierKind == query.SupplierKind),
            AssetDataResource.Suppliers when !string.IsNullOrWhiteSpace(query.Rating) =>
                db.Suppliers.AsNoTracking().Where(x => x.Rating == query.Rating),
            AssetDataResource.AssetStatuses when !string.IsNullOrWhiteSpace(query.StatusCategory) =>
                db.AssetStatuses.AsNoTracking().Where(x => x.StatusCategory == query.StatusCategory),
            AssetDataResource.CustomAttributeDefinitions when query.AssetTypeId.HasValue &&
                !string.IsNullOrWhiteSpace(query.DataType) &&
                Enum.TryParse<CustomAttributeDataType>(NormalizeDataType(query.DataType), true, out var scopedDataType) =>
                db.CustomAttributeDefinitions.AsNoTracking().Where(x => x.DataType == scopedDataType &&
                    x.AssetTypes.Any(a => a.AssetTypeId == query.AssetTypeId)),
            AssetDataResource.CustomAttributeDefinitions when query.AssetTypeId.HasValue =>
                db.CustomAttributeDefinitions.AsNoTracking().Where(x =>
                    x.AssetTypes.Any(a => a.AssetTypeId == query.AssetTypeId)),
            AssetDataResource.CustomAttributeDefinitions when !string.IsNullOrWhiteSpace(query.DataType) &&
                Enum.TryParse<CustomAttributeDataType>(NormalizeDataType(query.DataType), true, out var dataType) =>
                db.CustomAttributeDefinitions.AsNoTracking().Where(x => x.DataType == dataType),
            _ => source
        };
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
        else source = source.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Code.Contains(search) || x.Name.Contains(search) ||
                (x.AlternateName != null && x.AlternateName.Contains(search)));
        }
        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("name", "desc") => source.OrderByDescending(x => x.Name),
            ("name", _) => source.OrderBy(x => x.Name),
            ("createdatutc", "desc") => source.OrderByDescending(x => x.CreatedAtUtc),
            ("createdatutc", _) => source.OrderBy(x => x.CreatedAtUtc),
            (_, "desc") => source.OrderByDescending(x => x.Code),
            _ => source.OrderBy(x => x.Code)
        };
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return Page(rows.Select(ToDto).ToArray(), query, total);
    }

    public async Task<AssetDataDto> GetAsync(
        AssetDataResource resource, Guid id, CancellationToken cancellationToken) =>
        ToDto(await FindMasterAsync(resource, id, cancellationToken));

    public async Task<AssetDataDto> CreateAsync(
        AssetDataResource resource, MasterDataRequest request, CancellationToken cancellationToken)
    {
        if (await MasterQuery(resource).AnyAsync(x => x.Code == request.Code, cancellationToken))
            throw new ConflictException($"Code '{request.Code}' already exists.");
        var entity = CreateMaster(resource, request);
        await ValidateMasterReferencesAsync(entity, cancellationToken);
        db.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<AssetDataDto> UpdateAsync(
        AssetDataResource resource, Guid id, MasterDataRequest request, string? rowVersion,
        CancellationToken cancellationToken)
    {
        var entity = await FindMasterAsync(resource, id, cancellationToken);
        if (await MasterQuery(resource).AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
            throw new ConflictException($"Code '{request.Code}' already exists.");
        if (!string.IsNullOrWhiteSpace(rowVersion))
            db.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        if (entity is CustomAttributeDefinition definition)
        {
            var hasValues = await db.AssetTypeAttributes.AnyAsync(
                x => x.CustomAttributeDefinitionId == id && x.HasRecordedValues, cancellationToken);
            AssetDataRules.EnsureAttributeCodeCanChange(hasValues, definition.Code, request.Code);
        }
        ApplyMaster(entity, request);
        await ValidateMasterReferencesAsync(entity, cancellationToken);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeactivateAsync(
        AssetDataResource resource, Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindMasterAsync(resource, id, cancellationToken);
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<AssetDataDto> RestoreAsync(
        AssetDataResource resource, Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindMasterAsync(resource, id, cancellationToken);
        entity.IsActive = true;
        entity.DeletedAtUtc = null;
        entity.DeletedBy = null;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyCollection<LookupDto>> LookupAsync(
        AssetDataResource resource, string? search, CancellationToken cancellationToken)
    {
        var source = MasterQuery(resource).AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(
        AssetDataResource resource, string code, Guid? excludingId, CancellationToken cancellationToken) =>
        MasterQuery(resource).AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId), cancellationToken);

    public async Task<IReadOnlyCollection<AssetLookupDto>> AssetLookupAsync(string? search, CancellationToken cancellationToken)
    {
        var source = db.Assets.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.AssetNumber.Contains(search) || x.Name.Contains(search) || x.Code.Contains(search));
        return await source.OrderBy(x => x.Name).Take(50)
            .Select(x => new AssetLookupDto(x.Id, x.AssetNumber, x.Name)).ToArrayAsync(cancellationToken);
    }

    private IQueryable<CodedMasterEntity> MasterQuery(AssetDataResource resource) => resource switch
    {
        AssetDataResource.AssetTypes => db.AssetTypes,
        AssetDataResource.AssetCategories => db.AssetCategories,
        AssetDataResource.AssetModels => db.AssetModels,
        AssetDataResource.Manufacturers => db.Manufacturers,
        AssetDataResource.Suppliers => db.Suppliers,
        AssetDataResource.AssetStatuses => db.AssetStatuses,
        AssetDataResource.CustomAttributeDefinitions => db.CustomAttributeDefinitions,
        _ => throw new ArgumentOutOfRangeException(nameof(resource))
    };

    private async Task<CodedMasterEntity> FindMasterAsync(
        AssetDataResource resource, Guid id, CancellationToken cancellationToken) =>
        await MasterQuery(resource).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException($"{resource} record was not found.");

    private static CodedMasterEntity CreateMaster(AssetDataResource resource, MasterDataRequest request)
    {
        CodedMasterEntity entity = resource switch
        {
            AssetDataResource.AssetTypes => new AssetType(),
            AssetDataResource.AssetCategories => new AssetCategory(),
            AssetDataResource.AssetModels => new AssetModel(),
            AssetDataResource.Manufacturers => new Manufacturer(),
            AssetDataResource.Suppliers => new Supplier(),
            AssetDataResource.AssetStatuses => new AssetStatus(),
            AssetDataResource.CustomAttributeDefinitions => new CustomAttributeDefinition(),
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
        ApplyMaster(entity, request);
        return entity;
    }

    private static void ApplyMaster(CodedMasterEntity entity, MasterDataRequest request)
    {
        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.AlternateName = request.AlternateName?.Trim();
        entity.Description = request.Description?.Trim();
        switch (entity)
        {
            case AssetType x:
                x.AssetCategoryId = request.AssetCategoryId;
                x.RequiresSerialNumber = request.RequiresSerialNumber;
                x.RequiresRfidTag = request.RequiresRfidTag;
                x.RequiresBarcode = request.RequiresBarcode;
                x.DefaultStatusId = request.DefaultStatusId;
                x.PermittedStatusTransitions = request.PermittedStatusTransitions;
                x.CustomAttributeSchema = request.CustomAttributeSchema;
                x.DefaultDepreciationMethod = request.DefaultDepreciationMethod;
                x.DefaultUsefulLifeMonths = request.DefaultUsefulLifeMonths;
                x.NumberingFormat = request.NumberingFormat;
                break;
            case AssetCategory x:
                AssetDataRules.EnsureCategoryParentIsValid(x.Id, request.ParentId);
                x.ParentId = request.ParentId; x.AccountCode = request.AccountCode; break;
            case AssetModel x:
                x.ManufacturerId = request.ManufacturerId ?? Guid.Empty;
                x.AssetTypeId = request.AssetTypeId;
                x.ModelNumber = string.IsNullOrWhiteSpace(request.ModelNumber) ? "" : request.ModelNumber.Trim();
                x.Specifications = request.Specifications;
                x.ExpectedUsefulLifeMonths = request.ExpectedUsefulLifeMonths;
                x.Documentation = request.Documentation;
                break;
            case Manufacturer x:
                x.Country = request.Country; x.SupportContact = request.SupportContact; x.Website = request.Website; break;
            case Supplier x:
                x.SupplierKind = request.SupplierKind ?? "General";
                x.TaxRegistration = request.TaxRegistration; x.ContactPerson = request.ContactPerson;
                x.Telephone = request.Telephone; x.Email = request.Email; x.Address = request.Address;
                x.Country = request.Country; x.PaymentTerms = request.PaymentTerms;
                x.Rating = request.Rating; x.ExternalIdentifier = request.ExternalIdentifier; break;
            case AssetStatus x:
                x.StatusCategory = request.StatusCategory ?? "Unknown";
                x.Color = request.Color ?? "#6b7280";
                x.IsOperational = request.IsOperational;
                x.IsTerminal = request.IsTerminal;
                x.BlocksMovement = request.BlocksMovement;
                x.DisplayOrder = request.DisplayOrder;
                break;
            case CustomAttributeDefinition x:
                if (!Enum.TryParse<CustomAttributeDataType>(NormalizeDataType(request.DataType), true, out var type))
                    type = CustomAttributeDataType.Text;
                x.DataType = type;
                x.ListValuesJson = request.ListValues is null ? null : JsonSerializer.Serialize(request.ListValues);
                x.Unit = request.Unit; x.HelpText = request.HelpText; x.AlternateHelpText = request.AlternateHelpText;
                break;
        }
    }

    private async Task ValidateMasterReferencesAsync(CodedMasterEntity entity, CancellationToken cancellationToken)
    {
        if (entity is AssetType assetType)
        {
            if (assetType.AssetCategoryId.HasValue &&
                !await db.AssetCategories.AnyAsync(x => x.Id == assetType.AssetCategoryId && x.IsActive, cancellationToken))
                throw new NotFoundException("Asset category was not found.");
            if (assetType.DefaultStatusId.HasValue &&
                !await db.AssetStatuses.AnyAsync(x => x.Id == assetType.DefaultStatusId && x.IsActive, cancellationToken))
                throw new NotFoundException("Default asset status was not found.");
        }
        if (entity is AssetCategory category && category.ParentId.HasValue)
        {
            var seen = new HashSet<Guid> { category.Id };
            var currentId = category.ParentId;
            while (currentId.HasValue)
            {
                if (!seen.Add(currentId.Value))
                    throw new ConflictException("The category hierarchy would contain a cycle.");
                var parent = await db.AssetCategories.AsNoTracking().SingleOrDefaultAsync(
                    x => x.Id == currentId && x.IsActive, cancellationToken)
                    ?? throw new NotFoundException("A category ancestor was not found.");
                currentId = parent.ParentId;
            }
        }
        if (entity is AssetModel model)
        {
            if (model.ManufacturerId == Guid.Empty ||
                !await db.Manufacturers.AnyAsync(x => x.Id == model.ManufacturerId && x.IsActive, cancellationToken))
                throw new NotFoundException("Manufacturer was not found.");
            if (model.AssetTypeId.HasValue &&
                !await db.AssetTypes.AnyAsync(x => x.Id == model.AssetTypeId && x.IsActive, cancellationToken))
                throw new NotFoundException("Asset type was not found.");
        }
    }

    private static AssetDataDto ToDto(CodedMasterEntity entity)
    {
        var details = new Dictionary<string, object?>();
        switch (entity)
        {
            case AssetType x:
                details["assetCategoryId"] = x.AssetCategoryId;
                details["requiresSerialNumber"] = x.RequiresSerialNumber;
                details["requiresRfidTag"] = x.RequiresRfidTag;
                details["requiresBarcode"] = x.RequiresBarcode;
                details["defaultStatusId"] = x.DefaultStatusId;
                details["permittedStatusTransitions"] = x.PermittedStatusTransitions;
                details["customAttributeSchema"] = x.CustomAttributeSchema;
                details["defaultDepreciationMethod"] = x.DefaultDepreciationMethod;
                details["defaultUsefulLifeMonths"] = x.DefaultUsefulLifeMonths;
                details["numberingFormat"] = x.NumberingFormat;
                break;
            case AssetCategory x:
                details["parentId"] = x.ParentId; details["accountCode"] = x.AccountCode; break;
            case AssetModel x:
                details["manufacturerId"] = x.ManufacturerId; details["assetTypeId"] = x.AssetTypeId;
                details["modelNumber"] = x.ModelNumber; details["specifications"] = x.Specifications;
                details["expectedUsefulLifeMonths"] = x.ExpectedUsefulLifeMonths;
                details["documentation"] = x.Documentation;
                break;
            case Manufacturer x:
                details["country"] = x.Country; details["supportContact"] = x.SupportContact;
                details["website"] = x.Website; break;
            case Supplier x:
                details["supplierKind"] = x.SupplierKind; details["taxRegistration"] = x.TaxRegistration;
                details["contactPerson"] = x.ContactPerson; details["telephone"] = x.Telephone;
                details["email"] = x.Email; details["address"] = x.Address; details["country"] = x.Country;
                details["paymentTerms"] = x.PaymentTerms; details["rating"] = x.Rating;
                details["externalIdentifier"] = x.ExternalIdentifier; break;
            case AssetStatus x:
                details["statusCategory"] = x.StatusCategory; details["color"] = x.Color;
                details["isOperational"] = x.IsOperational; details["isTerminal"] = x.IsTerminal;
                details["blocksMovement"] = x.BlocksMovement; details["displayOrder"] = x.DisplayOrder; break;
            case CustomAttributeDefinition x:
                details["dataType"] = x.DataType.ToString();
                details["listValues"] = string.IsNullOrWhiteSpace(x.ListValuesJson)
                    ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(x.ListValuesJson);
                details["unit"] = x.Unit; details["helpText"] = x.HelpText;
                details["alternateHelpText"] = x.AlternateHelpText; break;
        }
        return new(entity.Id, entity.Code, entity.Name, entity.AlternateName, entity.Description,
            entity.IsActive, entity.CreatedAtUtc, entity.UpdatedAtUtc,
            entity.RowVersion.Length == 0 ? null : Convert.ToBase64String(entity.RowVersion), details);
    }

    private static string NormalizeDataType(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "yes or no" or "yes, no" or "yes/no" => nameof(CustomAttributeDataType.YesNo),
            _ => value?.Replace(" ", "") ?? nameof(CustomAttributeDataType.Text)
        };

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, ListQuery query, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(items, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

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
