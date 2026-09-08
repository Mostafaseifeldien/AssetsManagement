using System.Text.Json;
using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetDataService(
    AssetsDbContext db,
    IFileStorageService files,
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

    public async Task<IReadOnlyCollection<LookupDto>> GetAllowedStatusTransitionsAsync(
        Guid statusId, CancellationToken cancellationToken)
    {
        if (!await db.AssetStatuses.AnyAsync(x => x.Id == statusId, cancellationToken))
            throw new NotFoundException("Asset status was not found.");
        return await db.AssetStatusTransitions.AsNoTracking().Where(x => x.FromStatusId == statusId)
            .Select(x => new LookupDto(x.ToStatusId, x.ToStatus.Code, x.ToStatus.Name))
            .OrderBy(x => x.Name).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> SetAllowedStatusTransitionsAsync(
        Guid statusId, StatusTransitionRequest request, CancellationToken cancellationToken)
    {
        if (request.AllowedToStatusIds.Contains(statusId))
            throw new ConflictException("A status cannot transition to itself.");
        if (!await db.AssetStatuses.AnyAsync(x => x.Id == statusId && x.IsActive, cancellationToken))
            throw new NotFoundException("Asset status was not found.");
        var sourceStatus = await db.AssetStatuses.AsNoTracking().SingleAsync(x => x.Id == statusId, cancellationToken);
        if (sourceStatus.IsTerminal && request.AllowedToStatusIds.Count > 0)
            throw new ConflictException("A terminal status cannot have outgoing transitions.");
        var distinct = request.AllowedToStatusIds.Distinct().ToArray();
        var valid = await db.AssetStatuses.CountAsync(x => distinct.Contains(x.Id) && x.IsActive, cancellationToken);
        if (valid != distinct.Length) throw new NotFoundException("One or more target statuses were not found.");
        await db.AssetStatusTransitions.Where(x => x.FromStatusId == statusId).ExecuteDeleteAsync(cancellationToken);
        db.AssetStatusTransitions.AddRange(distinct.Select(x => new AssetStatusTransition
            { FromStatusId = statusId, ToStatusId = x }));
        await SaveAsync(cancellationToken);
        return await GetAllowedStatusTransitionsAsync(statusId, cancellationToken);
    }

    public async Task<PagedResult<TypeAttributeDto>> ListTypeAttributesAsync(
        ListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetTypeAttributes.AsNoTracking()
            .Include(x => x.AssetType).Include(x => x.CustomAttributeDefinition).AsQueryable();
        if (query.RelatedEntityId.HasValue)
            source = source.Where(x => x.AssetTypeId == query.RelatedEntityId);
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<AttributeRequirement>(query.Status, true, out var requirement))
            source = source.Where(x => x.Requirement == requirement);
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
        else source = source.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
            source = source.Where(x => x.CustomAttributeDefinition.Code.Contains(query.Search) ||
                x.CustomAttributeDefinition.Name.Contains(query.Search));
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderBy(x => x.AssetType.Name).ThenBy(x => x.DisplayOrder)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArrayAsync(cancellationToken);
        return Page(rows.Select(ToTypeAttributeDto).ToArray(), query, total);
    }

    public async Task<TypeAttributeDto> CreateTypeAttributeAsync(
        TypeAttributeRequest request, CancellationToken cancellationToken)
    {
        await EnsureTypeAndDefinitionAsync(request, cancellationToken);
        if (await db.AssetTypeAttributes.AnyAsync(x => x.AssetTypeId == request.AssetTypeId &&
            x.CustomAttributeDefinitionId == request.CustomAttributeDefinitionId, cancellationToken))
            throw new ConflictException("That attribute is already assigned to this asset type.");
        var entity = new AssetTypeAttribute();
        ApplyTypeAttribute(entity, request);
        db.AssetTypeAttributes.Add(entity);
        await SaveAsync(cancellationToken);
        return ToTypeAttributeDto(await TypeAttributeQuery(entity.Id).SingleAsync(cancellationToken));
    }

    public async Task<TypeAttributeDto> CreateTypeAttributeDefinitionAsync(
        Guid assetTypeId, CreateTypeAttributeDefinitionRequest request, CancellationToken cancellationToken)
    {
        if (!await db.AssetTypes.AnyAsync(x => x.Id == assetTypeId && x.IsActive, cancellationToken))
            throw new NotFoundException("Asset type was not found.");
        if (await db.AssetTypeAttributes.AnyAsync(x => x.AssetTypeId == assetTypeId &&
            x.CustomAttributeDefinition.Code == request.Code, cancellationToken))
            throw new ConflictException("That attribute code already exists on this asset type.");
        if (await db.AssetTypeAttributes.AnyAsync(x => x.AssetTypeId == assetTypeId &&
            x.DisplayOrder == request.DisplayOrder && x.IsActive, cancellationToken))
            throw new ConflictException("That display order is already used on this asset type.");
        var definition = new CustomAttributeDefinition
        {
            Code = request.Code.Trim(), Name = request.Label.Trim(), AlternateName = request.AlternateName,
            DataType = Enum.TryParse<CustomAttributeDataType>(NormalizeDataType(request.DataType), true, out var type)
                ? type : CustomAttributeDataType.Text,
            ListValuesJson = request.ListValues is null ? null : JsonSerializer.Serialize(request.ListValues),
            Unit = request.Unit, HelpText = request.HelpText, AlternateHelpText = request.AlternateHelpText
        };
        var assignment = new AssetTypeAttribute
        {
            AssetTypeId = assetTypeId, CustomAttributeDefinition = definition,
            Requirement = Enum.Parse<AttributeRequirement>(request.Requirement),
            DisplayOrder = request.DisplayOrder, ShowInList = request.ShowInList
        };
        db.AssetTypeAttributes.Add(assignment);
        await SaveAsync(cancellationToken);
        return ToTypeAttributeDto(await TypeAttributeQuery(assignment.Id).SingleAsync(cancellationToken));
    }

    public async Task<TypeAttributeDto> GetTypeAttributeAsync(Guid id, CancellationToken cancellationToken) =>
        ToTypeAttributeDto(await TypeAttributeQuery(id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Type attribute was not found."));

    public async Task<TypeAttributeDto> UpdateTypeAttributeAsync(
        Guid id, TypeAttributeRequest request, CancellationToken cancellationToken)
    {
        await EnsureTypeAndDefinitionAsync(request, cancellationToken);
        var entity = await db.AssetTypeAttributes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Type attribute was not found.");
        if (await db.AssetTypeAttributes.AnyAsync(x => x.Id != id && x.AssetTypeId == request.AssetTypeId &&
            x.CustomAttributeDefinitionId == request.CustomAttributeDefinitionId, cancellationToken))
            throw new ConflictException("That attribute is already assigned to this asset type.");
        ApplyTypeAttribute(entity, request);
        await SaveAsync(cancellationToken);
        return ToTypeAttributeDto(await TypeAttributeQuery(entity.Id).SingleAsync(cancellationToken));
    }

    public async Task DeactivateTypeAttributeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.AssetTypeAttributes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Type attribute was not found.");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<TypeAttributeDto> RestoreTypeAttributeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.AssetTypeAttributes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Type attribute was not found.");
        entity.IsActive = true;
        entity.DeletedAtUtc = null;
        entity.DeletedBy = null;
        await SaveAsync(cancellationToken);
        return ToTypeAttributeDto(await TypeAttributeQuery(id).SingleAsync(cancellationToken));
    }

    public Task<PagedResult<IdentifierDto>> ListRfidTagsAsync(ListQuery query, CancellationToken cancellationToken) =>
        ListIdentifiersAsync(true, query, cancellationToken);

    public async Task<IdentifierDto> GetRfidTagAsync(Guid id, CancellationToken cancellationToken) =>
        ToDto(await RfidQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found."));

    public async Task<IdentifierDto> CreateRfidTagAsync(RfidTagRequest request, CancellationToken cancellationToken)
    {
        if (await db.RfidTags.AnyAsync(x => x.TagIdentifier == request.TagIdentifier, cancellationToken))
            throw new ConflictException("That RFID tag identifier already exists and cannot be reused.");
        var entity = new RfidTag
        {
            TagIdentifier = request.TagIdentifier, TagType = request.TagType,
            EncodingStandard = request.EncodingStandard
        };
        db.RfidTags.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IdentifierDto> UpdateRfidTagAsync(Guid id, RfidTagRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.RfidTags.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found.");
        if (entity.Status != IdentifierStatus.Unassigned)
            throw new ConflictException("An assigned or retired tag cannot be edited.");
        if (await db.RfidTags.AnyAsync(x => x.Id != id && x.TagIdentifier == request.TagIdentifier, cancellationToken))
            throw new ConflictException("That RFID tag identifier already exists and cannot be reused.");
        entity.TagIdentifier = request.TagIdentifier;
        entity.TagType = request.TagType;
        entity.EncodingStandard = request.EncodingStandard;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<IdentifierDto> AssignRfidTagAsync(Guid id, Guid assetId, CancellationToken cancellationToken) =>
        AssignRfidAsync(id, assetId, cancellationToken);

    public async Task<IdentifierDto> UnassignRfidTagAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.RfidTags.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found.");
        if (entity.Status != IdentifierStatus.Assigned)
            throw new ConflictException("Only an assigned tag can be unassigned.");
        entity.AssetId = null;
        entity.Status = IdentifierStatus.Unassigned;
        entity.EncodedAtUtc = null;
        entity.EncodedBy = null;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IdentifierDto> ReplaceRfidTagAsync(Guid id, Guid replacementId, CancellationToken cancellationToken)
    {
        var old = await db.RfidTags.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found.");
        if (old.Status != IdentifierStatus.Assigned || old.AssetId is null)
            throw new ConflictException("Only an assigned tag can be replaced.");
        if (id == replacementId) throw new ConflictException("A tag cannot replace itself.");
        var assetId = old.AssetId.Value;
        old.AssetId = null;
        old.Status = IdentifierStatus.Retired;
        old.RetiredAtUtc = DateTime.UtcNow;
        old.ReplacedById = replacementId;
        var replacement = await db.RfidTags.FindAsync([replacementId], cancellationToken)
            ?? throw new NotFoundException("Replacement RFID tag was not found.");
        AssetDataRules.EnsureIdentifierCanBeAssigned(replacement.Status, replacement.AssetId);
        replacement.AssetId = assetId;
        replacement.Status = IdentifierStatus.Assigned;
        replacement.EncodedAtUtc = DateTime.UtcNow;
        replacement.EncodedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
        return ToDto(replacement);
    }

    public async Task DeactivateRfidTagAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.RfidTags.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found.");
        if (entity.Status == IdentifierStatus.Assigned)
            throw new ConflictException("Unassign or replace the RFID tag before deactivating it.");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<IdentifierDto> RestoreRfidTagAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.RfidTags.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found.");
        entity.IsActive = true;
        entity.DeletedAtUtc = null;
        entity.DeletedBy = null;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<bool> RfidExistsAsync(string value, CancellationToken cancellationToken) =>
        db.RfidTags.AnyAsync(x => x.TagIdentifier == value, cancellationToken);

    public Task<PagedResult<IdentifierDto>> ListBarcodesAsync(ListQuery query, CancellationToken cancellationToken) =>
        ListIdentifiersAsync(false, query, cancellationToken);

    public async Task<IdentifierDto> GetBarcodeAsync(Guid id, CancellationToken cancellationToken) =>
        ToDto(await BarcodeQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Barcode was not found."));

    public async Task<IdentifierDto> CreateBarcodeAsync(BarcodeRequest request, CancellationToken cancellationToken)
    {
        if (await db.Barcodes.AnyAsync(x => x.Value == request.Value && x.Symbology == request.Symbology, cancellationToken))
            throw new ConflictException("That barcode value and symbology already exist and cannot be reused.");
        var entity = new Barcode { Value = request.Value, Symbology = request.Symbology, SubjectType = request.SubjectType };
        db.Barcodes.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IdentifierDto> UpdateBarcodeAsync(Guid id, BarcodeRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Barcodes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Barcode was not found.");
        if (entity.Status != IdentifierStatus.Unassigned)
            throw new ConflictException("An assigned or retired barcode cannot be edited.");
        if (await db.Barcodes.AnyAsync(x => x.Id != id && x.Value == request.Value &&
            x.Symbology == request.Symbology, cancellationToken))
            throw new ConflictException("That barcode value and symbology already exist and cannot be reused.");
        entity.Value = request.Value;
        entity.Symbology = request.Symbology;
        entity.SubjectType = request.SubjectType;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IdentifierDto> GenerateBarcodeAsync(string symbology, CancellationToken cancellationToken)
    {
        if (symbology is not ("Code128" or "Code39" or "QR" or "DataMatrix" or "EAN"))
            throw new ConflictException("Unsupported barcode symbology.");
        string value;
        do value = $"AST-{Guid.NewGuid():N}".ToUpperInvariant()[..20];
        while (await db.Barcodes.AnyAsync(x => x.Value == value && x.Symbology == symbology, cancellationToken));
        return await CreateBarcodeAsync(new BarcodeRequest { Value = value, Symbology = symbology }, cancellationToken);
    }

    public async Task<IdentifierDto> AssignBarcodeAsync(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        var entity = await db.Barcodes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Barcode was not found.");
        AssetDataRules.EnsureIdentifierCanBeAssigned(entity.Status, entity.AssetId);
        await EnsureAssetAvailableAsync(assetId, cancellationToken);
        if (await db.Barcodes.AnyAsync(x => x.AssetId == assetId && x.Status == IdentifierStatus.Assigned, cancellationToken))
            throw new ConflictException("The asset already has an assigned barcode.");
        entity.AssetId = assetId;
        entity.Status = IdentifierStatus.Assigned;
        entity.PrintedAtUtc = DateTime.UtcNow;
        await SaveAsync(cancellationToken);
        return ToDto(await BarcodeQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    public async Task<IdentifierDto> UnassignBarcodeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Barcodes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Barcode was not found.");
        if (entity.Status != IdentifierStatus.Assigned)
            throw new ConflictException("Only an assigned barcode can be unassigned.");
        entity.AssetId = null;
        entity.Status = IdentifierStatus.Unassigned;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IdentifierDto> ReplaceBarcodeAsync(Guid id, Guid replacementId, CancellationToken cancellationToken)
    {
        var old = await db.Barcodes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Barcode was not found.");
        if (old.Status != IdentifierStatus.Assigned || old.AssetId is null)
            throw new ConflictException("Only an assigned barcode can be replaced.");
        if (id == replacementId) throw new ConflictException("A barcode cannot replace itself.");
        var assetId = old.AssetId.Value;
        old.AssetId = null;
        old.Status = IdentifierStatus.Retired;
        old.ReplacedById = replacementId;
        var replacement = await db.Barcodes.FindAsync([replacementId], cancellationToken)
            ?? throw new NotFoundException("Replacement barcode was not found.");
        AssetDataRules.EnsureIdentifierCanBeAssigned(replacement.Status, replacement.AssetId);
        replacement.AssetId = assetId;
        replacement.Status = IdentifierStatus.Assigned;
        replacement.PrintedAtUtc = DateTime.UtcNow;
        await SaveAsync(cancellationToken);
        return ToDto(replacement);
    }

    public async Task DeactivateBarcodeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Barcodes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Barcode was not found.");
        if (entity.Status == IdentifierStatus.Assigned)
            throw new ConflictException("Unassign or replace the barcode before deactivating it.");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
    }

    public async Task<IdentifierDto> RestoreBarcodeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Barcodes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Barcode was not found.");
        entity.IsActive = true;
        entity.DeletedAtUtc = null;
        entity.DeletedBy = null;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<bool> BarcodeExistsAsync(string value, CancellationToken cancellationToken) =>
        db.Barcodes.AnyAsync(x => x.Value == value, cancellationToken);

    public async Task<PagedResult<AssetImageDto>> ListImagesAsync(ListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetImages.AsNoTracking().Include(x => x.Asset).Where(x => x.IsActive);
        if (query.RelatedEntityId.HasValue) source = source.Where(x => x.AssetId == query.RelatedEntityId);
        if (!string.IsNullOrWhiteSpace(query.Search))
            source = source.Where(x => x.OriginalFileName.Contains(query.Search) ||
                (x.Caption != null && x.Caption.Contains(query.Search)) || x.Asset.Name.Contains(query.Search));
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderByDescending(x => x.IsPrimary).ThenByDescending(x => x.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArrayAsync(cancellationToken);
        return Page(rows.Select(ToImageDto).ToArray(), query, total);
    }

    public async Task<AssetImageDto> GetImageAsync(Guid id, CancellationToken cancellationToken) =>
        ToImageDto(await ImageQuery(id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Asset image was not found."));

    public async Task<AssetImageDto> UploadImageAsync(Guid assetId, ImageUpload upload, CancellationToken cancellationToken)
    {
        await EnsureAssetAvailableAsync(assetId, cancellationToken);
        EnsureImagePurpose(upload.Purpose);
        var extension = Path.GetExtension(upload.OriginalFileName);
        var stored = await files.SaveAsync(upload.Content, extension, cancellationToken);
        var entity = new AssetImage
        {
            AssetId = assetId, StoredFileName = stored,
            OriginalFileName = Path.GetFileName(upload.OriginalFileName),
            ContentType = upload.ContentType, SizeBytes = upload.SizeBytes,
            Caption = upload.Caption, Purpose = upload.Purpose,
            CapturedAtUtc = DateTime.UtcNow, CapturedBy = currentUser.UserName
        };
        var hasPrimary = await db.AssetImages.AnyAsync(x => x.AssetId == assetId && x.IsPrimary && x.IsActive, cancellationToken);
        entity.IsPrimary = upload.IsPrimary || !hasPrimary;
        if (entity.IsPrimary)
            await db.AssetImages.Where(x => x.AssetId == assetId && x.IsPrimary)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsPrimary, false), cancellationToken);
        db.AssetImages.Add(entity);
        try { await SaveAsync(cancellationToken); }
        catch { await files.DeleteAsync(stored, cancellationToken); throw; }
        return await GetImageAsync(entity.Id, cancellationToken);
    }

    public async Task<AssetImageDto> UpdateImageAsync(
        Guid id, string? caption, string? purpose, CancellationToken cancellationToken)
    {
        var entity = await db.AssetImages.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset image was not found.");
        if (string.Equals(entity.Purpose, "Damage evidence", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Damage-evidence photographs are locked and cannot be changed.");
        EnsureImagePurpose(purpose);
        entity.Caption = caption;
        entity.Purpose = purpose;
        await SaveAsync(cancellationToken);
        return await GetImageAsync(id, cancellationToken);
    }

    public async Task<AssetImageDto> SetPrimaryImageAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.AssetImages.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset image was not found.");
        if (string.Equals(entity.Purpose, "Damage evidence", StringComparison.OrdinalIgnoreCase) ||
            await db.AssetImages.AnyAsync(x => x.AssetId == entity.AssetId && x.IsPrimary &&
                x.Purpose == "Damage evidence" && x.Id != id, cancellationToken))
            throw new ConflictException("Damage-evidence photographs are locked and cannot be changed.");
        await db.AssetImages.Where(x => x.AssetId == entity.AssetId && x.Id != id && x.IsPrimary)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsPrimary, false), cancellationToken);
        entity.IsPrimary = true;
        await SaveAsync(cancellationToken);
        return await GetImageAsync(id, cancellationToken);
    }

    public async Task DeleteImageAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.AssetImages.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset image was not found.");
        if (string.Equals(entity.Purpose, "Damage evidence", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Damage-evidence photographs are locked and cannot be removed.");
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        entity.IsPrimary = false;
        await SaveAsync(cancellationToken);
        await files.DeleteAsync(entity.StoredFileName, cancellationToken);
        var replacement = await db.AssetImages.Where(x => x.AssetId == entity.AssetId && x.IsActive && x.Id != id)
            .OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (replacement is not null)
        {
            replacement.IsPrimary = true;
            await SaveAsync(cancellationToken);
        }
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenImageAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.AssetImages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Asset image was not found.");
        return (await files.OpenReadAsync(entity.StoredFileName, cancellationToken), entity.ContentType, entity.OriginalFileName);
    }

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
                x.ModelNumber = request.ModelNumber?.Trim() ?? "";
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
            if (string.IsNullOrWhiteSpace(model.ModelNumber))
                throw new DomainRuleException("Model number is required.");
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

    private IQueryable<AssetTypeAttribute> TypeAttributeQuery(Guid id) =>
        db.AssetTypeAttributes.AsNoTracking().Include(x => x.AssetType)
            .Include(x => x.CustomAttributeDefinition).Where(x => x.Id == id);

    private static TypeAttributeDto ToTypeAttributeDto(AssetTypeAttribute x) =>
        new(x.Id, x.AssetTypeId, x.AssetType.Name, x.CustomAttributeDefinitionId,
            x.CustomAttributeDefinition.Code, x.CustomAttributeDefinition.Name,
            x.CustomAttributeDefinition.DataType.ToString(), x.Requirement.ToString(),
            x.DisplayOrder, x.ShowInList, x.IsActive);

    private async Task EnsureTypeAndDefinitionAsync(TypeAttributeRequest request, CancellationToken cancellationToken)
    {
        if (!await db.AssetTypes.AnyAsync(x => x.Id == request.AssetTypeId && x.IsActive, cancellationToken))
            throw new NotFoundException("Asset type was not found.");
        if (!await db.CustomAttributeDefinitions.AnyAsync(
            x => x.Id == request.CustomAttributeDefinitionId && x.IsActive, cancellationToken))
            throw new NotFoundException("Custom attribute definition was not found.");
    }

    private static void ApplyTypeAttribute(AssetTypeAttribute entity, TypeAttributeRequest request)
    {
        entity.AssetTypeId = request.AssetTypeId;
        entity.CustomAttributeDefinitionId = request.CustomAttributeDefinitionId;
        entity.Requirement = Enum.Parse<AttributeRequirement>(request.Requirement);
        entity.DisplayOrder = request.DisplayOrder;
        entity.ShowInList = request.ShowInList;
    }

    private IQueryable<RfidTag> RfidQuery() => db.RfidTags.AsNoTracking().Include(x => x.Asset);
    private IQueryable<Barcode> BarcodeQuery() => db.Barcodes.AsNoTracking().Include(x => x.Asset);

    private async Task<PagedResult<IdentifierDto>> ListIdentifiersAsync(
        bool rfid, ListQuery query, CancellationToken cancellationToken)
    {
        if (rfid)
        {
            var source = RfidQuery();
            if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
            else source = source.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(query.Search))
                source = source.Where(x => x.TagIdentifier.Contains(query.Search));
            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<IdentifierStatus>(query.Status, true, out var status))
                source = source.Where(x => x.Status == status);
            if (query.RelatedEntityId.HasValue) source = source.Where(x => x.AssetId == query.RelatedEntityId);
            var total = await source.CountAsync(cancellationToken);
            var rows = await source.OrderBy(x => x.TagIdentifier).Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize).ToArrayAsync(cancellationToken);
            return Page(rows.Select(ToDto).ToArray(), query, total);
        }
        else
        {
            var source = BarcodeQuery();
            if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
            else source = source.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(query.Search)) source = source.Where(x => x.Value.Contains(query.Search));
            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<IdentifierStatus>(query.Status, true, out var status))
                source = source.Where(x => x.Status == status);
            if (query.RelatedEntityId.HasValue) source = source.Where(x => x.AssetId == query.RelatedEntityId);
            var total = await source.CountAsync(cancellationToken);
            var rows = await source.OrderBy(x => x.Value).Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize).ToArrayAsync(cancellationToken);
            return Page(rows.Select(ToDto).ToArray(), query, total);
        }
    }

    private async Task<IdentifierDto> AssignRfidAsync(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        var entity = await db.RfidTags.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("RFID tag was not found.");
        AssetDataRules.EnsureIdentifierCanBeAssigned(entity.Status, entity.AssetId);
        await EnsureAssetAvailableAsync(assetId, cancellationToken);
        if (await db.RfidTags.AnyAsync(x => x.AssetId == assetId && x.Status == IdentifierStatus.Assigned, cancellationToken))
            throw new ConflictException("The asset already has an assigned RFID tag.");
        entity.AssetId = assetId;
        entity.Status = IdentifierStatus.Assigned;
        entity.EncodedAtUtc = DateTime.UtcNow;
        entity.EncodedBy = currentUser.UserName;
        await SaveAsync(cancellationToken);
        return ToDto(await RfidQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    private async Task EnsureAssetAvailableAsync(Guid assetId, CancellationToken cancellationToken)
    {
        if (!await db.Assets.AnyAsync(x => x.Id == assetId && x.IsActive, cancellationToken))
            throw new NotFoundException("Asset was not found.");
    }

    private static IdentifierDto ToDto(RfidTag x) =>
        new(x.Id, x.TagIdentifier, x.TagType, x.Status.ToString(), x.AssetId, x.Asset?.Name,
            x.EncodedAtUtc, x.IsActive);

    private static IdentifierDto ToDto(Barcode x) =>
        new(x.Id, x.Value, x.Symbology, x.Status.ToString(), x.AssetId, x.Asset?.Name,
            x.PrintedAtUtc, x.IsActive);

    private IQueryable<AssetImage> ImageQuery(Guid id) =>
        db.AssetImages.AsNoTracking().Include(x => x.Asset).Where(x => x.Id == id && x.IsActive);

    private static AssetImageDto ToImageDto(AssetImage x) =>
        new(x.Id, x.AssetId, x.Asset.Name, x.OriginalFileName, x.ContentType, x.SizeBytes,
            x.Caption, x.Purpose, x.IsPrimary,
            string.Equals(x.Purpose, "Damage evidence", StringComparison.OrdinalIgnoreCase),
            $"/api/asset-images/{x.Id}/content", x.CreatedAtUtc);

    private static void EnsureImagePurpose(string? purpose)
    {
        if (!string.IsNullOrWhiteSpace(purpose) &&
            purpose is not ("Identification" or "Condition record" or "Damage evidence" or "Nameplate"))
            throw new DomainRuleException("Image purpose must be Identification, Condition record, Damage evidence, or Nameplate.");
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
