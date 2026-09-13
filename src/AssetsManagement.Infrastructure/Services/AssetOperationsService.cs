using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetOperationsService(AssetsDbContext db, ICurrentUser currentUser, IAssetService assets)
    : IAssetOperationsService
{
    private const string Source = "Screen";

    public async Task<AssetScreenDto> GetScreenAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await assets.GetAsync(assetId, cancellationToken);
        var financials = await GetFinancialsAsync(assetId, cancellationToken);
        var maintenance = await GetMaintenanceAsync(assetId, cancellationToken);
        var identity = await GetIdentityAsync(assetId, cancellationToken);
        var movement = await GetMovementsAsync(assetId, cancellationToken);
        var custody = await db.CustodyAssignments.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.AssignedFromUtc)
            .Select(x => new CustodyAssignmentListItemDto(
                x.Id, x.AssetId, x.CustodianId, x.CustodianType, x.AssignedFromUtc,
                x.AssignedToUtc, x.AssignmentReason, x.Status))
            .ToArrayAsync(cancellationToken);
        var documents = await db.AssetDocumentLinks.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .Select(x => x.AssetDocument)
            .OrderByDescending(x => x.DocumentDate)
            .Select(x => new AssetScreenDocumentDto(
                x.Id, x.Title, x.DocumentKind, x.DocumentDate, x.ExpiresOn,
                x.IssuedBy, x.ReferenceNumber, x.Confidential, x.State))
            .ToArrayAsync(cancellationToken);
        var photos = await db.AssetImages.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive)
            .OrderByDescending(x => x.IsPrimary).ThenByDescending(x => x.CapturedAtUtc)
            .Select(x => new AssetScreenPhotoDto(
                x.Id, x.OriginalFileName, x.IsPrimary, x.Purpose, x.Caption, x.CapturedAtUtc))
            .ToArrayAsync(cancellationToken);
        return new(asset, financials, maintenance, identity, movement, custody, documents, photos);
    }

    public async Task<AssetFinancialsDto> GetFinancialsAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(assetId, cancellationToken);
        var expenses = await db.AssetExpenses.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.ExpenseDate)
            .Select(x => new AssetExpenseListItemDto(
                x.Id, x.AssetId, x.ExpenseKind, x.Amount, x.Currency, x.ExpenseDate, x.Capitalized, x.State))
            .ToArrayAsync(cancellationToken);
        var recorded = expenses.Where(x => x.State == ExpenseStates.Recorded).ToArray();
        var purchaseFromLedger = recorded.Where(x => x.ExpenseKind == "Purchase").Sum(x => x.Amount);
        var purchase = purchaseFromLedger > 0 ? purchaseFromLedger : asset.PurchaseValue ?? 0;
        var capAdd = recorded.Where(x => x.Capitalized && x.ExpenseKind != "Purchase").Sum(x => x.Amount);
        var operating = recorded.Where(x => !x.Capitalized).Sum(x => x.Amount);
        var capitalizedTotal = purchase + capAdd;
        var schedule = await ActiveScheduleAsync(assetId, cancellationToken);
        var written = schedule?.AccumulatedDepreciation ?? 0;
        var depreciable = schedule?.AcquisitionValue ?? capitalizedTotal;
        var book = schedule?.NetBookValue ?? Math.Max(0, capitalizedTotal - written);
        var monthly = schedule is null ? 0 : CurrentCharge(schedule);
        var warranty = await db.Warranties.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.State != WarrantyStates.Void)
            .OrderByDescending(x => x.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
        var claims = await db.WarrantyClaims.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.RaisedOn)
            .Select(x => new WarrantyClaimListItemDto(
                x.Id, x.ClaimNumber, x.AssetId, x.WarrantyId, x.FaultDescription, x.RaisedOn, x.State))
            .ToArrayAsync(cancellationToken);
        var warrantyDto = warranty is null ? null : MapWarranty(warranty);
        var dep = schedule is null
            ? null
            : $"{schedule.Method} over {schedule.UsefulLifeMonths} months";
        return new(
            assetId, purchase + capAdd + operating, written, book, monthly,
            purchase, capAdd, capitalizedTotal, operating, depreciable, recorded.Length,
            schedule is not null && Math.Abs(capitalizedTotal - depreciable) >= 2,
            asset.PurchaseDate, asset.PurchaseValue, asset.SupplierId, asset.CostCenter, dep,
            warrantyDto, warrantyDto is { State: WarrantyStates.Active or WarrantyStates.Expiring },
            expenses, claims);
    }

    public async Task<AssetMaintenanceDto> GetMaintenanceAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(assetId, cancellationToken);
        var jobs = await db.WorkOrders.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WorkOrderListItemDto(
                x.Id, x.WorkOrderNumber, x.AssetId, x.WorkKind, x.WorkDone, x.CompletedAtUtc,
                x.TotalCost, x.UnderWarranty, x.State))
            .ToArrayAsync(cancellationToken);
        var faults = await db.MaintenanceRequests.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.RaisedOnUtc)
            .Select(x => new MaintenanceRequestListItemDto(
                x.Id, x.RequestNumber, x.AssetId, x.FaultDescription, x.Urgency, x.RaisedOnUtc, x.State))
            .ToArrayAsync(cancellationToken);
        var inspections = await db.AssetInspections.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.InspectedOn)
            .Select(x => new AssetInspectionListItemDto(
                x.Id, x.AssetId, x.InspectionKind, x.InspectedOn, x.Condition, x.Findings, x.ActionRequired))
            .ToArrayAsync(cancellationToken);
        var plans = await CoveringPlansAsync(asset, cancellationToken);
        var downtime = await db.WorkOrders.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .SumAsync(x => x.DowntimeHours, cancellationToken);
        return new(
            assetId, jobs.Length, jobs.Count(x => WorkOrderStates.Open.Contains(x.State)),
            jobs.Where(x => !x.UnderWarranty).Sum(x => x.TotalCost),
            downtime, plans.Count, jobs, faults, inspections, plans);
    }

    public async Task<AssetIdentityDto> GetIdentityAsync(Guid assetId, CancellationToken cancellationToken)
    {
        await RequireAssetAsync(assetId, cancellationToken);
        var tag = await db.RfidTags.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.Status == IdentifierStatus.Assigned)
            .Select(x => new { x.Id, x.TagIdentifier, x.EncodedAtUtc })
            .FirstOrDefaultAsync(cancellationToken);
        var barcode = await db.Barcodes.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.Status == IdentifierStatus.Assigned)
            .Select(x => new { x.Id, x.Value, x.Symbology })
            .FirstOrDefaultAsync(cancellationToken);
        return new(
            assetId, tag is not null, tag?.Id, tag?.TagIdentifier, tag?.EncodedAtUtc,
            barcode is not null, barcode?.Id, barcode?.Value, barcode?.Symbology);
    }

    public async Task<AssetMovementDto> GetMovementsAsync(Guid assetId, CancellationToken cancellationToken)
    {
        await RequireAssetAsync(assetId, cancellationToken);
        var rows = (await db.AssetPositions.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.RecordedAtUtc)
            .ToArrayAsync(cancellationToken))
            .Select(MapPosition)
            .ToArray();
        return new(assetId, rows.FirstOrDefault(x => x.IsCurrent), rows);
    }

    public async Task<AssetPositionDto?> GetMapPositionAsync(Guid assetId, CancellationToken cancellationToken) =>
        (await GetMovementsAsync(assetId, cancellationToken)).Current;

    public Task<AssetSpecificationDto> GetSpecificationAsync() =>
        Task.FromResult(AssetSpecification.Sbo048);

    public async Task<PagedResult<AssetExpenseListItemDto>> ListExpensesAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetExpenses.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.State)) source = source.Where(x => x.State == query.State);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.ExpenseKind.Contains(search) ||
                (x.InvoiceNumber != null && x.InvoiceNumber.Contains(search)) ||
                (x.Notes != null && x.Notes.Contains(search)));
        }
        source = query.SortDirection == "asc"
            ? source.OrderBy(x => x.ExpenseDate)
            : source.OrderByDescending(x => x.ExpenseDate);
        return await PageAsync(source.Select(x => new AssetExpenseListItemDto(
            x.Id, x.AssetId, x.ExpenseKind, x.Amount, x.Currency, x.ExpenseDate, x.Capitalized, x.State)),
            query, cancellationToken);
    }

    public async Task<AssetExpenseDetailDto> GetExpenseAsync(Guid id, CancellationToken cancellationToken) =>
        MapExpense(await FindExpenseAsync(id, cancellationToken));

    public async Task<AssetExpenseDetailDto> CreateExpenseAsync(
        AssetExpenseRequest request, CancellationToken cancellationToken)
    {
        await RequireAssetAsync(request.AssetId, cancellationToken);
        await RequireOptionalSupplierAsync(request.SupplierId, cancellationToken);
        var entity = new AssetExpense
        {
            AssetId = request.AssetId!.Value,
            ExpenseKind = request.ExpenseKind!,
            Amount = request.Amount!.Value,
            Currency = request.Currency ?? "EGP",
            ExpenseDate = request.ExpenseDate!.Value,
            SupplierId = request.SupplierId,
            InvoiceNumber = NullIfEmpty(request.InvoiceNumber),
            Capitalized = request.Capitalized == true,
            CostCenter = NullIfEmpty(request.CostCenter),
            WorkOrderId = request.WorkOrderId,
            DocumentId = request.DocumentId,
            Notes = NullIfEmpty(request.Notes),
            State = request.State is ExpenseStates.Draft ? ExpenseStates.Draft : ExpenseStates.Recorded
        };
        db.AssetExpenses.Add(entity);
        AddHistory(nameof(AssetExpense), entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return MapExpense(entity);
    }

    public async Task<AssetExpenseDetailDto> UpdateExpenseAsync(
        Guid id, AssetExpenseRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindExpenseAsync(id, cancellationToken);
        AssetDataRules.EnsureExpenseCanBeEdited(entity.State);
        Track(nameof(AssetExpense), entity.Id, "Amount", entity.Amount.ToString("0.00"), request.Amount?.ToString("0.00"));
        entity.ExpenseKind = request.ExpenseKind ?? entity.ExpenseKind;
        entity.Amount = request.Amount ?? entity.Amount;
        entity.Currency = request.Currency ?? entity.Currency;
        if (request.ExpenseDate.HasValue) entity.ExpenseDate = request.ExpenseDate.Value;
        entity.SupplierId = request.SupplierId;
        entity.InvoiceNumber = NullIfEmpty(request.InvoiceNumber);
        if (request.Capitalized.HasValue) entity.Capitalized = request.Capitalized.Value;
        entity.CostCenter = NullIfEmpty(request.CostCenter);
        entity.WorkOrderId = request.WorkOrderId;
        entity.DocumentId = request.DocumentId;
        entity.Notes = NullIfEmpty(request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return MapExpense(entity);
    }

    public async Task<AssetExpenseDetailDto> ReverseExpenseAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindExpenseAsync(id, cancellationToken);
        AssetDataRules.EnsureExpenseCanBeReversed(entity.State);
        entity.State = ExpenseStates.Reversed;
        AddHistory(nameof(AssetExpense), entity.Id, "State changed from 'Recorded' to 'Reversed'");
        await db.SaveChangesAsync(cancellationToken);
        return MapExpense(entity);
    }

    public Task<IReadOnlyCollection<OperationHistoryDto>> GetExpenseHistoryAsync(
        Guid id, CancellationToken cancellationToken) =>
        HistoryAsync(nameof(AssetExpense), id, cancellationToken);

    public async Task<PagedResult<WarrantyListItemDto>> ListWarrantiesAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.Warranties.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.State)) source = source.Where(x => x.State == query.State);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Provider.Contains(search) ||
                (x.ReferenceNumber != null && x.ReferenceNumber.Contains(search)));
        }
        source = source.OrderByDescending(x => x.EndDate);
        return await PageAsync(source.Select(x => new WarrantyListItemDto(
            x.Id, x.AssetId, x.WarrantyKind, x.Provider, x.StartDate, x.EndDate, x.State)),
            query, cancellationToken);
    }

    public async Task<WarrantyDetailDto> GetWarrantyAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindWarrantyAsync(id, cancellationToken);
        RefreshWarranty(entity);
        await db.SaveChangesAsync(cancellationToken);
        return MapWarranty(entity);
    }

    public async Task<WarrantyDetailDto> CreateWarrantyAsync(
        WarrantyRequest request, CancellationToken cancellationToken)
    {
        await RequireAssetAsync(request.AssetId, cancellationToken);
        var provider = await RequireSupplierAsync(request.ProviderId, cancellationToken);
        var exists = await db.Warranties.AnyAsync(x =>
            x.AssetId == request.AssetId && x.WarrantyKind == request.WarrantyKind &&
            x.StartDate == request.StartDate && x.State != WarrantyStates.Void, cancellationToken);
        if (exists)
            throw new ConflictException("A warranty of this kind already starts on that date for the asset.");
        var entity = new Warranty
        {
            AssetId = request.AssetId!.Value,
            WarrantyKind = request.WarrantyKind!,
            ProviderId = provider.Id,
            Provider = provider.Name,
            ReferenceNumber = NullIfEmpty(request.ReferenceNumber),
            StartDate = request.StartDate!.Value,
            EndDate = request.EndDate!.Value,
            Coverage = NullIfEmpty(request.Coverage),
            Exclusions = NullIfEmpty(request.Exclusions),
            ResponseTime = NullIfEmpty(request.ResponseTime),
            Cost = request.Cost,
            DocumentId = request.DocumentId
        };
        RefreshWarranty(entity);
        db.Warranties.Add(entity);
        AddHistory(nameof(Warranty), entity.Id, "Record created");
        var asset = await db.Assets.SingleAsync(x => x.Id == entity.AssetId, cancellationToken);
        if (asset.WarrantyExpiry is null || entity.EndDate > asset.WarrantyExpiry)
            asset.WarrantyExpiry = entity.EndDate;
        await db.SaveChangesAsync(cancellationToken);
        return MapWarranty(entity);
    }

    public async Task<WarrantyDetailDto> UpdateWarrantyAsync(
        Guid id, WarrantyRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindWarrantyAsync(id, cancellationToken);
        if (entity.State == WarrantyStates.Void)
            throw new DomainRuleException("A void warranty cannot be edited.");
        var provider = await RequireSupplierAsync(request.ProviderId, cancellationToken);
        entity.WarrantyKind = request.WarrantyKind ?? entity.WarrantyKind;
        entity.ProviderId = provider.Id;
        entity.Provider = provider.Name;
        entity.ReferenceNumber = NullIfEmpty(request.ReferenceNumber);
        if (request.StartDate.HasValue) entity.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) entity.EndDate = request.EndDate.Value;
        entity.Coverage = NullIfEmpty(request.Coverage);
        entity.Exclusions = NullIfEmpty(request.Exclusions);
        entity.ResponseTime = NullIfEmpty(request.ResponseTime);
        entity.Cost = request.Cost;
        entity.DocumentId = request.DocumentId;
        RefreshWarranty(entity);
        await db.SaveChangesAsync(cancellationToken);
        return MapWarranty(entity);
    }

    public async Task<WarrantyDetailDto> VoidWarrantyAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindWarrantyAsync(id, cancellationToken);
        entity.State = WarrantyStates.Void;
        AddHistory(nameof(Warranty), entity.Id, "State changed to 'Void'");
        await db.SaveChangesAsync(cancellationToken);
        return MapWarranty(entity);
    }

    public async Task<PagedResult<WarrantyClaimListItemDto>> ListClaimsAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.WarrantyClaims.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.State)) source = source.Where(x => x.State == query.State);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.ClaimNumber.Contains(search) || x.FaultDescription.Contains(search));
        }
        source = source.OrderByDescending(x => x.RaisedOn);
        return await PageAsync(source.Select(x => new WarrantyClaimListItemDto(
            x.Id, x.ClaimNumber, x.AssetId, x.WarrantyId, x.FaultDescription, x.RaisedOn, x.State)),
            query, cancellationToken);
    }

    public async Task<WarrantyClaimDetailDto> GetClaimAsync(Guid id, CancellationToken cancellationToken) =>
        MapClaim(await FindClaimAsync(id, cancellationToken));

    public async Task<WarrantyClaimDetailDto> CreateClaimAsync(
        WarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(request.AssetId, cancellationToken);
        var warranty = await ResolveClaimableWarrantyAsync(asset.Id, request.WarrantyId, cancellationToken);
        if (request.RaisedById.HasValue && request.RaisedById != Guid.Empty)
            await RequireEmployeeAsync(request.RaisedById, "Raised by", cancellationToken);
        var entity = new WarrantyClaim
        {
            ClaimNumber = await NextNumberAsync("WCL", db.WarrantyClaims.CountAsync(cancellationToken)),
            WarrantyId = warranty.Id,
            AssetId = asset.Id,
            RaisedOn = DateTime.UtcNow.Date,
            RaisedById = request.RaisedById is { } raisedBy && raisedBy != Guid.Empty ? raisedBy : null,
            FaultDescription = request.FaultDescription!.Trim(),
            WorkOrderId = request.WorkOrderId,
            ProviderReference = NullIfEmpty(request.ProviderReference) ?? NullIfEmpty(warranty.ReferenceNumber),
            AmountClaimed = request.AmountClaimed,
            State = WarrantyClaimStates.Submitted
        };
        db.WarrantyClaims.Add(entity);
        AddHistory(nameof(WarrantyClaim), entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return MapClaim(entity);
    }

    public async Task<WarrantyClaimDetailDto> SettleClaimAsync(
        Guid id, WarrantyClaimDecisionRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindClaimAsync(id, cancellationToken);
        if (entity.State is WarrantyClaimStates.Settled or WarrantyClaimStates.Rejected or WarrantyClaimStates.Withdrawn)
            throw new DomainRuleException("This claim is already closed.");
        entity.AmountRecovered = request.AmountRecovered;
        entity.Resolution = NullIfEmpty(request.Resolution);
        entity.State = WarrantyClaimStates.Settled;
        entity.ClosedOn = DateTime.UtcNow.Date;
        AddHistory(nameof(WarrantyClaim), entity.Id, "State changed to 'Settled'");
        await db.SaveChangesAsync(cancellationToken);
        return MapClaim(entity);
    }

    public async Task<WarrantyClaimDetailDto> RejectClaimAsync(
        Guid id, WarrantyClaimDecisionRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindClaimAsync(id, cancellationToken);
        if (entity.State is WarrantyClaimStates.Settled or WarrantyClaimStates.Rejected or WarrantyClaimStates.Withdrawn)
            throw new DomainRuleException("This claim is already closed.");
        entity.Resolution = NullIfEmpty(request.Resolution);
        entity.State = WarrantyClaimStates.Rejected;
        entity.ClosedOn = DateTime.UtcNow.Date;
        AddHistory(nameof(WarrantyClaim), entity.Id, "State changed to 'Rejected'");
        await db.SaveChangesAsync(cancellationToken);
        return MapClaim(entity);
    }

    public async Task<PagedResult<DepreciationScheduleListItemDto>> ListSchedulesAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.DepreciationSchedules.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.State)) source = source.Where(x => x.State == query.State);
        source = source.OrderByDescending(x => x.StartDate);
        return await PageAsync(source.Select(x => new DepreciationScheduleListItemDto(
            x.Id, x.AssetId, x.Method, x.UsefulLifeMonths, x.AcquisitionValue,
            x.AccumulatedDepreciation, x.NetBookValue, x.State)), query, cancellationToken);
    }

    public async Task<DepreciationScheduleDetailDto> GetScheduleAsync(Guid id, CancellationToken cancellationToken) =>
        MapSchedule(await FindScheduleAsync(id, cancellationToken));

    public async Task<DepreciationScheduleDetailDto> GetAssetScheduleAsync(
        Guid assetId, CancellationToken cancellationToken)
    {
        await RequireAssetAsync(assetId, cancellationToken);
        var schedule = await ActiveScheduleAsync(assetId, cancellationToken)
            ?? throw new NotFoundException("No depreciation schedule was found for this asset.");
        return MapSchedule(schedule);
    }

    public async Task<DepreciationScheduleDetailDto> CreateScheduleAsync(
        DepreciationScheduleRequest request, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(request.AssetId, cancellationToken);
        if (await db.DepreciationSchedules.AnyAsync(x =>
            x.AssetId == asset.Id &&
            (x.State == DepreciationStates.Draft || x.State == DepreciationStates.Running ||
             x.State == DepreciationStates.Suspended), cancellationToken))
            throw new ConflictException("An asset may have only one active depreciation schedule.");
        var entity = BuildSchedule(asset.Id, request);
        db.DepreciationSchedules.Add(entity);
        AddHistory(nameof(DepreciationSchedule), entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return MapSchedule(entity);
    }

    public async Task<DepreciationScheduleDetailDto> SupersedeScheduleAsync(
        Guid id, DepreciationScheduleRequest request, CancellationToken cancellationToken)
    {
        var current = await FindScheduleAsync(id, cancellationToken);
        AssetDataRules.EnsureScheduleIsActive(current.State);
        request = new DepreciationScheduleRequest
        {
            AssetId = current.AssetId,
            Method = request.Method ?? current.Method,
            AcquisitionValue = request.AcquisitionValue ?? current.AcquisitionValue,
            ResidualValue = request.ResidualValue ?? current.ResidualValue,
            UsefulLifeMonths = request.UsefulLifeMonths ?? current.UsefulLifeMonths,
            StartDate = request.StartDate ?? DateTime.UtcNow.Date,
            Rate = request.Rate ?? current.Rate,
            PeriodLength = request.PeriodLength ?? current.PeriodLength
        };
        var next = BuildSchedule(current.AssetId, request);
        current.State = DepreciationStates.Superseded;
        current.SupersededById = next.Id;
        db.DepreciationSchedules.Add(next);
        AddHistory(nameof(DepreciationSchedule), current.Id, "State changed to 'Superseded'");
        AddHistory(nameof(DepreciationSchedule), next.Id, "Record created as a revaluation");
        await db.SaveChangesAsync(cancellationToken);
        return MapSchedule(next);
    }

    public async Task<PagedResult<MaintenancePlanDto>> ListPlansAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.MaintenancePlans.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        }
        source = source.OrderBy(x => x.Code);
        return await PageAsync(source.Select(x => new MaintenancePlanDto(
            x.Id, x.Code, x.Name, x.ScopeKind, x.ScopeId, x.TriggerKind, x.TaskList, x.IntervalMonths, x.IsActive)),
            query, cancellationToken);
    }

    public async Task<MaintenancePlanDto> GetPlanAsync(Guid id, CancellationToken cancellationToken) =>
        MapPlan(await FindPlanAsync(id, cancellationToken));

    public async Task<MaintenancePlanDto> CreatePlanAsync(
        MaintenancePlanRequest request, CancellationToken cancellationToken)
    {
        if (await db.MaintenancePlans.AnyAsync(x => x.Code == request.Code, cancellationToken))
            throw new ConflictException("A maintenance plan with this code already exists.");
        var entity = new MaintenancePlan
        {
            Code = request.Code!.Trim(),
            Name = request.Name!.Trim(),
            ScopeKind = request.ScopeKind!,
            ScopeId = request.ScopeId,
            TriggerKind = string.IsNullOrWhiteSpace(request.TriggerKind) ? "Calendar interval" : request.TriggerKind,
            TaskList = request.TaskList,
            IntervalMonths = request.IntervalMonths
        };
        db.MaintenancePlans.Add(entity);
        AddHistory(nameof(MaintenancePlan), entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return MapPlan(entity);
    }

    public async Task<MaintenancePlanDto> UpdatePlanAsync(
        Guid id, MaintenancePlanRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindPlanAsync(id, cancellationToken);
        entity.Name = request.Name?.Trim() ?? entity.Name;
        entity.ScopeKind = request.ScopeKind ?? entity.ScopeKind;
        entity.ScopeId = request.ScopeId ?? entity.ScopeId;
        entity.TriggerKind = request.TriggerKind ?? entity.TriggerKind;
        entity.TaskList = request.TaskList ?? entity.TaskList;
        entity.IntervalMonths = request.IntervalMonths ?? entity.IntervalMonths;
        await db.SaveChangesAsync(cancellationToken);
        return MapPlan(entity);
    }

    public async Task<PagedResult<MaintenanceRequestListItemDto>> ListRequestsAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.MaintenanceRequests.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.State)) source = source.Where(x => x.State == query.State);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.RequestNumber.Contains(search) || x.FaultDescription.Contains(search));
        }
        source = source.OrderByDescending(x => x.RaisedOnUtc);
        return await PageAsync(source.Select(x => new MaintenanceRequestListItemDto(
            x.Id, x.RequestNumber, x.AssetId, x.FaultDescription, x.Urgency, x.RaisedOnUtc, x.State)),
            query, cancellationToken);
    }

    public async Task<MaintenanceRequestDetailDto> GetRequestAsync(Guid id, CancellationToken cancellationToken) =>
        MapMaintRequest(await FindMaintRequestAsync(id, cancellationToken));

    public async Task<MaintenanceRequestDetailDto> CreateRequestAsync(
        MaintenanceRequestCreate request, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(request.AssetId, cancellationToken);
        await RequireEmployeeAsync(request.RaisedById, "Raised by", cancellationToken);
        var urgency = request.Urgency ?? (request.AssetUsable == false ? "Asset stopped" : "Normal");
        var entity = new MaintenanceRequest
        {
            RequestNumber = await NextNumberAsync("MR", db.MaintenanceRequests.CountAsync(cancellationToken)),
            AssetId = asset.Id,
            RaisedById = request.RaisedById,
            RaisedOnUtc = DateTime.UtcNow,
            FaultDescription = request.FaultDescription!.Trim(),
            Urgency = urgency,
            AssetUsable = request.AssetUsable != false,
            PhotographId = request.PhotographId,
            LocationAtReport = NullIfEmpty(request.LocationAtReport) ?? asset.CurrentLocation
        };
        db.MaintenanceRequests.Add(entity);
        AddHistory(nameof(MaintenanceRequest), entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return MapMaintRequest(entity);
    }

    public async Task<MaintenanceRequestDetailDto> RejectRequestAsync(
        Guid id, MaintenanceRejectRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindMaintRequestAsync(id, cancellationToken);
        AssetDataRules.EnsureRequestCanBeRejected(entity.State);
        entity.State = MaintenanceRequestStates.Rejected;
        entity.RejectionReason = request.RejectionReason!.Trim();
        AddHistory(nameof(MaintenanceRequest), entity.Id, "State changed to 'Rejected'");
        await db.SaveChangesAsync(cancellationToken);
        return MapMaintRequest(entity);
    }

    public async Task<WorkOrderDetailDto> ConvertRequestAsync(
        Guid id, MaintenanceConvertRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindMaintRequestAsync(id, cancellationToken);
        AssetDataRules.EnsureRequestCanBeConverted(entity.State);
        var wo = await CreateWorkOrderCoreAsync(new WorkOrderRequest
        {
            AssetId = entity.AssetId,
            WorkKind = request.WorkKind ?? WorkKinds.All[1],
            Source = "Maintenance request",
            SourceRequestId = entity.Id,
            Priority = request.Priority ?? (entity.AssetUsable ? "Normal" : "High"),
            AssignedToId = request.AssignedToId,
            ScheduledStart = request.ScheduledStart,
            DueDate = request.DueDate,
            AssetOutOfService = !entity.AssetUsable
        }, cancellationToken);
        entity.State = MaintenanceRequestStates.Converted;
        entity.WorkOrderId = wo.Id;
        AddHistory(nameof(MaintenanceRequest), entity.Id, $"Converted to work order '{wo.WorkOrderNumber}'");
        await db.SaveChangesAsync(cancellationToken);
        return await GetWorkOrderAsync(wo.Id, cancellationToken);
    }

    public async Task<PagedResult<WorkOrderListItemDto>> ListWorkOrdersAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.WorkOrders.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.State)) source = source.Where(x => x.State == query.State);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.WorkOrderNumber.Contains(search) ||
                x.WorkKind.Contains(search) || (x.WorkDone != null && x.WorkDone.Contains(search)));
        }
        source = source.OrderByDescending(x => x.CreatedAtUtc);
        return await PageAsync(source.Select(x => new WorkOrderListItemDto(
            x.Id, x.WorkOrderNumber, x.AssetId, x.WorkKind, x.WorkDone, x.CompletedAtUtc,
            x.TotalCost, x.UnderWarranty, x.State)), query, cancellationToken);
    }

    public async Task<WorkOrderDetailDto> GetWorkOrderAsync(Guid id, CancellationToken cancellationToken) =>
        MapWorkOrder(await FindWorkOrderAsync(id, cancellationToken));

    public async Task<WorkOrderDetailDto> CreateWorkOrderAsync(
        WorkOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await CreateWorkOrderCoreAsync(request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return MapWorkOrder(entity);
    }

    public async Task<WorkOrderDetailDto> UpdateWorkOrderAsync(
        Guid id, WorkOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindWorkOrderAsync(id, cancellationToken);
        AssetDataRules.EnsureWorkOrderIsOpen(entity.State);
        entity.WorkKind = request.WorkKind ?? entity.WorkKind;
        entity.Priority = request.Priority ?? entity.Priority;
        entity.ScheduledStart = request.ScheduledStart ?? entity.ScheduledStart;
        entity.DueDate = request.DueDate ?? entity.DueDate;
        entity.AssignedToId = request.AssignedToId ?? entity.AssignedToId;
        entity.ProviderId = request.ProviderId ?? entity.ProviderId;
        if (request.UnderWarranty.HasValue) entity.UnderWarranty = request.UnderWarranty.Value;
        if (request.AssetOutOfService.HasValue) entity.AssetOutOfService = request.AssetOutOfService.Value;
        entity.WorkDone = NullIfEmpty(request.WorkDone) ?? entity.WorkDone;
        await db.SaveChangesAsync(cancellationToken);
        return MapWorkOrder(entity);
    }

    public async Task<WorkOrderDetailDto> StartWorkOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindWorkOrderAsync(id, cancellationToken);
        AssetDataRules.EnsureWorkOrderIsOpen(entity.State);
        entity.State = WorkOrderStates.InProgress;
        entity.StartedAtUtc ??= DateTime.UtcNow;
        if (entity.AssetOutOfService)
            await SetAssetStatusAsync(entity.AssetId, "MNT", "In Maintenance", cancellationToken);
        AddHistory(nameof(WorkOrder), entity.Id, "State changed to 'In progress'");
        await db.SaveChangesAsync(cancellationToken);
        return MapWorkOrder(entity);
    }

    public async Task<WorkOrderDetailDto> CompleteWorkOrderAsync(
        Guid id, WorkOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindWorkOrderAsync(id, cancellationToken);
        AssetDataRules.EnsureWorkOrderIsOpen(entity.State);
        entity.WorkDone = NullIfEmpty(request.WorkDone) ?? entity.WorkDone;
        entity.CompletedAtUtc = DateTime.UtcNow;
        entity.StartedAtUtc ??= entity.CompletedAtUtc;
        entity.DowntimeHours = entity.AssetOutOfService
            ? Math.Round((decimal)(entity.CompletedAtUtc.Value - entity.StartedAtUtc.Value).TotalHours, 1)
            : 0;
        RecalcCost(entity);
        entity.State = WorkOrderStates.Completed;
        if (entity.AssetOutOfService)
            await SetAssetStatusAsync(entity.AssetId, "WRK", "Working", cancellationToken);
        AddHistory(nameof(WorkOrder), entity.Id, "State changed to 'Completed'");
        await db.SaveChangesAsync(cancellationToken);
        return MapWorkOrder(entity);
    }

    public async Task<WorkOrderDetailDto> AddWorkOrderLineAsync(
        Guid id, WorkOrderLineRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindWorkOrderAsync(id, cancellationToken);
        AssetDataRules.EnsureWorkOrderIsOpen(entity.State);
        var qty = request.Hours ?? request.Quantity ?? 1;
        var unit = request.UnitCost ?? 0;
        var line = new WorkOrderLine
        {
            WorkOrderId = entity.Id,
            LineNumber = entity.Lines.Count == 0 ? 1 : entity.Lines.Max(x => x.LineNumber) + 1,
            LineKind = request.LineKind!,
            Description = request.Description!.Trim(),
            TechnicianId = request.TechnicianId,
            Hours = request.Hours,
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            LineCost = Math.Round(qty * unit, 2),
            Chargeable = request.Chargeable != false && !entity.UnderWarranty
        };
        entity.Lines.Add(line);
        RecalcCost(entity);
        AddHistory(nameof(WorkOrder), entity.Id, $"Line {line.LineNumber} added");
        await db.SaveChangesAsync(cancellationToken);
        return MapWorkOrder(entity);
    }

    public async Task<PagedResult<AssetInspectionListItemDto>> ListInspectionsAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetInspections.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.InspectionKind.Contains(search) ||
                x.Condition.Contains(search) || (x.Findings != null && x.Findings.Contains(search)));
        }
        source = source.OrderByDescending(x => x.InspectedOn);
        return await PageAsync(source.Select(x => new AssetInspectionListItemDto(
            x.Id, x.AssetId, x.InspectionKind, x.InspectedOn, x.Condition, x.Findings, x.ActionRequired)),
            query, cancellationToken);
    }

    public async Task<AssetInspectionDetailDto> GetInspectionAsync(Guid id, CancellationToken cancellationToken) =>
        MapInspection(await FindInspectionAsync(id, cancellationToken));

    public async Task<AssetInspectionDetailDto> CreateInspectionAsync(
        AssetInspectionRequest request, CancellationToken cancellationToken)
    {
        await RequireAssetAsync(request.AssetId, cancellationToken);
        await RequireEmployeeAsync(request.InspectorId, "Inspector", cancellationToken);
        var entity = new AssetInspection
        {
            AssetId = request.AssetId!.Value,
            InspectionKind = request.InspectionKind!,
            InspectedOn = request.InspectedOn!.Value,
            InspectorId = request.InspectorId!.Value,
            Condition = request.Condition!,
            LocationConfirmed = request.LocationConfirmed == true,
            CustodianConfirmed = request.CustodianConfirmed == true,
            Findings = NullIfEmpty(request.Findings),
            PhotographId = request.PhotographId,
            ActionRequired = request.ActionRequired ?? "None",
            NextDue = request.NextDue
        };
        db.AssetInspections.Add(entity);
        AddHistory(nameof(AssetInspection), entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return MapInspection(entity);
    }

    public async Task<PagedResult<AssetPositionDto>> ListPositionsAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.AssetPositions.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.FloorPlan.Contains(search) ||
                (x.Room != null && x.Room.Contains(search)));
        }
        source = source.OrderByDescending(x => x.RecordedAtUtc);
        return await PageAsync(source.Select(x => new AssetPositionDto(
            x.Id, x.AssetId, x.FloorPlan, x.Room, x.X, x.Y, x.PositionSource, x.Confidence,
            x.RecordedAtUtc, x.RecordedBy, x.DerivedFromReader, x.Valid, x.IsCurrent)),
            query, cancellationToken);
    }

    public async Task<AssetPositionDto> GetPositionAsync(Guid id, CancellationToken cancellationToken) =>
        MapPosition(await FindPositionAsync(id, cancellationToken));

    public async Task<AssetPositionDto> CreatePositionAsync(
        AssetPositionRequest request, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(request.AssetId, cancellationToken);
        var current = await db.AssetPositions
            .Where(x => x.AssetId == asset.Id && x.IsCurrent)
            .ToArrayAsync(cancellationToken);
        foreach (var item in current)
            item.IsCurrent = false;
        var entity = new AssetPosition
        {
            AssetId = asset.Id,
            FloorPlan = request.FloorPlan!.Trim(),
            Room = NullIfEmpty(request.Room),
            X = request.X!.Value,
            Y = request.Y!.Value,
            PositionSource = request.PositionSource ?? "Manual",
            Confidence = request.Confidence,
            RecordedAtUtc = DateTime.UtcNow,
            RecordedBy = currentUser.DisplayName,
            DerivedFromReader = NullIfEmpty(request.DerivedFromReader),
            Valid = true,
            IsCurrent = true
        };
        db.AssetPositions.Add(entity);
        asset.CurrentLocation = string.IsNullOrWhiteSpace(entity.Room)
            ? entity.FloorPlan
            : $"{entity.Room}, {entity.FloorPlan}";
        asset.LocationUpdatedAtUtc = entity.RecordedAtUtc;
        asset.LocationSource = entity.PositionSource == "Manual" ? "Manual" : "Reader";
        AddHistory(nameof(AssetPosition), entity.Id, "Current position recorded");
        await db.SaveChangesAsync(cancellationToken);
        return MapPosition(entity);
    }

    public async Task<PagedResult<ExitAuthorizationListItemDto>> ListExitsAsync(
        AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var source = db.ExitAuthorizations.AsNoTracking().AsQueryable();
        if (query.AssetId.HasValue) source = source.Where(x => x.AssetId == query.AssetId);
        if (!string.IsNullOrWhiteSpace(query.State)) source = source.Where(x => x.Status == query.State);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Number.Contains(search) ||
                (x.Reason != null && x.Reason.Contains(search)) ||
                (x.Destination != null && x.Destination.Contains(search)));
        }
        source = source.OrderByDescending(x => x.RequestedAtUtc);
        return await PageAsync(source.Select(x => new ExitAuthorizationListItemDto(
            x.Id, x.Number, x.AssetId, x.Reason, x.Destination, x.RequestedAtUtc, x.Status)),
            query, cancellationToken);
    }

    public async Task<ExitAuthorizationDetailDto> GetExitAsync(Guid id, CancellationToken cancellationToken) =>
        MapExit(await FindExitAsync(id, cancellationToken));

    public async Task<ExitAuthorizationDetailDto> CreateExitAsync(
        ExitAuthorizationRequest request, CancellationToken cancellationToken)
    {
        await RequireAssetAsync(request.AssetId, cancellationToken);
        await RequireEmployeeAsync(request.RequestedById, "Requested by", cancellationToken);
        var entity = new ExitAuthorization
        {
            Number = await NextNumberAsync("EXA", db.ExitAuthorizations.CountAsync(cancellationToken)),
            Status = ExitAuthorizationStates.PendingOwner,
            AssetId = request.AssetId!.Value,
            RequestedById = request.RequestedById!.Value,
            RequestedAtUtc = DateTime.UtcNow,
            Reason = request.Reason,
            Destination = NullIfEmpty(request.Destination),
            ExpectedReturn = request.ExpectedReturn,
            CarrierId = request.CarrierId,
            GateScope = NullIfEmpty(request.GateScope)
        };
        db.ExitAuthorizations.Add(entity);
        AddHistory(nameof(ExitAuthorization), entity.Id, "Record created");
        await db.SaveChangesAsync(cancellationToken);
        return MapExit(entity);
    }

    public async Task<ExitAuthorizationDetailDto> DecideExitAsync(
        Guid id, ExitDecisionRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindExitAsync(id, cancellationToken);
        AssetDataRules.EnsureExitCanBeDecided(entity.Status);
        await RequireEmployeeAsync(request.ApproverId, "Approver", cancellationToken);
        var now = DateTime.UtcNow;
        var expected = request.Level switch
        {
            ExitApprovalLevels.Owner => ExitAuthorizationStates.PendingOwner,
            ExitApprovalLevels.Manager => ExitAuthorizationStates.PendingManager,
            _ => ExitAuthorizationStates.PendingSecurity
        };
        if (entity.Status != expected)
            throw new DomainRuleException($"This authorization is waiting for the {entity.Status} step.");
        if (request.Decision == ApprovalDecisions.Rejected)
        {
            ApplyDecision(entity, request.Level!, request.ApproverId!.Value, ApprovalDecisions.Rejected, now);
            entity.Status = ExitAuthorizationStates.Rejected;
            AddHistory(nameof(ExitAuthorization), entity.Id, $"{request.Level} rejected the exit");
            await db.SaveChangesAsync(cancellationToken);
            return MapExit(entity);
        }
        ApplyDecision(entity, request.Level!, request.ApproverId!.Value, ApprovalDecisions.Approved, now);
        entity.Status = request.Level switch
        {
            ExitApprovalLevels.Owner => ExitAuthorizationStates.PendingManager,
            ExitApprovalLevels.Manager => ExitAuthorizationStates.PendingSecurity,
            _ => ExitAuthorizationStates.Approved
        };
        if (entity.Status == ExitAuthorizationStates.Approved)
        {
            entity.ValidFromUtc = now;
            entity.ValidToUtc = entity.ExpectedReturn ?? now.AddDays(7);
            entity.Status = ExitAuthorizationStates.Active;
        }
        AddHistory(nameof(ExitAuthorization), entity.Id, $"{request.Level} approved the exit");
        await db.SaveChangesAsync(cancellationToken);
        return MapExit(entity);
    }

    public async Task<ExitAuthorizationDetailDto> ReturnExitAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindExitAsync(id, cancellationToken);
        if (entity.Status is not (ExitAuthorizationStates.Active or ExitAuthorizationStates.Approved
            or ExitAuthorizationStates.Used))
            throw new DomainRuleException("Only an approved or active exit can be marked as returned.");
        entity.ReturnedAtUtc = DateTime.UtcNow;
        entity.UsedAtUtc ??= entity.ReturnedAtUtc;
        entity.Status = ExitAuthorizationStates.Used;
        AddHistory(nameof(ExitAuthorization), entity.Id, "Asset marked as returned");
        await db.SaveChangesAsync(cancellationToken);
        return MapExit(entity);
    }

    private async Task<WorkOrder> CreateWorkOrderCoreAsync(
        WorkOrderRequest request, CancellationToken cancellationToken)
    {
        var asset = await RequireAssetAsync(request.AssetId, cancellationToken);
        var underWarranty = request.UnderWarranty ?? await db.Warranties.AnyAsync(x =>
            x.AssetId == asset.Id && x.State != WarrantyStates.Void &&
            x.StartDate <= DateTime.UtcNow.Date && x.EndDate >= DateTime.UtcNow.Date, cancellationToken);
        var entity = new WorkOrder
        {
            WorkOrderNumber = await NextNumberAsync("WO", db.WorkOrders.CountAsync(cancellationToken)),
            AssetId = asset.Id,
            WorkKind = request.WorkKind!,
            Source = request.Source ?? "Manual",
            SourceRequestId = request.SourceRequestId,
            Priority = request.Priority ?? "Normal",
            ScheduledStart = request.ScheduledStart,
            DueDate = request.DueDate,
            AssignedToId = request.AssignedToId,
            ProviderId = request.ProviderId,
            UnderWarranty = underWarranty,
            AssetOutOfService = request.AssetOutOfService == true,
            WorkDone = NullIfEmpty(request.WorkDone),
            State = WorkOrderStates.Scheduled
        };
        db.WorkOrders.Add(entity);
        AddHistory(nameof(WorkOrder), entity.Id, "Record created");
        if (entity.AssetOutOfService)
            await SetAssetStatusAsync(asset.Id, "MNT", "In Maintenance", cancellationToken);
        return entity;
    }

    private async Task SetAssetStatusAsync(
        Guid assetId, string code, string name, CancellationToken cancellationToken)
    {
        var status = await db.AssetStatuses.FirstOrDefaultAsync(x => x.Code == code || x.Name == name, cancellationToken);
        if (status is null) return;
        var asset = await db.Assets.SingleAsync(x => x.Id == assetId, cancellationToken);
        if (asset.AssetStatusId == status.Id) return;
        asset.AssetStatusId = status.Id;
    }

    private static void RecalcCost(WorkOrder entity) =>
        entity.TotalCost = entity.Lines.Where(x => x.Chargeable).Sum(x => x.LineCost);

    private static DepreciationSchedule BuildSchedule(Guid assetId, DepreciationScheduleRequest request)
    {
        var entity = new DepreciationSchedule
        {
            AssetId = assetId,
            Method = request.Method!,
            AcquisitionValue = request.AcquisitionValue!.Value,
            ResidualValue = request.ResidualValue ?? 0,
            UsefulLifeMonths = request.UsefulLifeMonths!.Value,
            StartDate = request.StartDate!.Value,
            Rate = request.Rate,
            PeriodLength = request.PeriodLength ?? "Monthly",
            State = DepreciationStates.Running
        };
        GenerateEntries(entity);
        return entity;
    }

    private static void GenerateEntries(DepreciationSchedule schedule)
    {
        var months = schedule.PeriodLength switch
        {
            "Quarterly" => 3,
            "Annual" => 12,
            _ => 1
        };
        var periods = Math.Max(1, (int)Math.Ceiling(schedule.UsefulLifeMonths / (double)months));
        var remaining = schedule.AcquisitionValue - schedule.ResidualValue;
        var opening = schedule.AcquisitionValue;
        var posted = 0m;
        var today = DateTime.UtcNow.Date;
        for (var i = 0; i < periods; i++)
        {
            var start = schedule.StartDate.AddMonths(i * months);
            var end = start.AddMonths(months).AddDays(-1);
            decimal charge;
            if (schedule.Method == "Not depreciated")
                charge = 0;
            else if (schedule.Method == "Reducing balance")
            {
                var annual = (schedule.Rate ?? 20) / 100m;
                var rate = annual * months / 12m;
                charge = Math.Round(Math.Min(opening * rate, Math.Max(0, opening - schedule.ResidualValue)), 2);
            }
            else
            {
                var straight = Math.Round(remaining / (periods - i), 2);
                charge = Math.Min(straight, Math.Max(0, opening - schedule.ResidualValue));
                remaining -= charge;
            }
            var closing = Math.Max(schedule.ResidualValue, opening - charge);
            charge = opening - closing;
            var state = end < today ? "Posted" : "Projected";
            if (state == "Posted") posted += charge;
            schedule.Entries.Add(new DepreciationEntry
            {
                Period = start.ToString("yyyy-MM"),
                PeriodStart = start,
                PeriodEnd = end,
                OpeningValue = opening,
                Charge = charge,
                ClosingValue = closing,
                State = state
            });
            opening = closing;
        }
        schedule.AccumulatedDepreciation = posted;
        schedule.NetBookValue = schedule.AcquisitionValue - posted;
        if (schedule.Entries.All(x => x.State == "Posted"))
            schedule.State = DepreciationStates.Completed;
    }

    private static decimal CurrentCharge(DepreciationSchedule schedule)
    {
        var today = DateTime.UtcNow.Date;
        return schedule.Entries
            .Where(x => x.PeriodStart <= today && x.PeriodEnd >= today)
            .Select(x => x.Charge)
            .FirstOrDefault();
    }

    private async Task<IReadOnlyCollection<MaintenancePlanDto>> CoveringPlansAsync(
        Asset asset, CancellationToken cancellationToken)
    {
        return await db.MaintenancePlans.AsNoTracking()
            .Where(x => x.IsActive && (
                (x.ScopeKind == "Single asset" && x.ScopeId == asset.Id) ||
                (x.ScopeKind == "Asset type" && x.ScopeId == asset.AssetTypeId) ||
                (x.ScopeKind == "Asset category" && x.ScopeId == asset.AssetCategoryId)))
            .OrderBy(x => x.Name)
            .Select(x => new MaintenancePlanDto(
                x.Id, x.Code, x.Name, x.ScopeKind, x.ScopeId, x.TriggerKind, x.TaskList, x.IntervalMonths, x.IsActive))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<DepreciationSchedule?> ActiveScheduleAsync(Guid assetId, CancellationToken cancellationToken) =>
        await db.DepreciationSchedules.Include(x => x.Entries)
            .Where(x => x.AssetId == assetId && x.State != DepreciationStates.Superseded)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

    private static void RefreshWarranty(Warranty entity)
    {
        if (entity.State == WarrantyStates.Void) return;
        var today = DateTime.UtcNow.Date;
        entity.State = entity.EndDate.Date < today
            ? WarrantyStates.Expired
            : entity.EndDate.Date <= today.AddDays(90)
                ? WarrantyStates.Expiring
                : WarrantyStates.Active;
    }

    private static void ApplyDecision(
        ExitAuthorization entity, string level, Guid approverId, string decision, DateTime when)
    {
        if (level == ExitApprovalLevels.Owner)
        {
            entity.OwnerApproverId = approverId;
            entity.OwnerDecision = decision;
            entity.OwnerDecidedAtUtc = when;
        }
        else if (level == ExitApprovalLevels.Manager)
        {
            entity.ManagerApproverId = approverId;
            entity.ManagerDecision = decision;
            entity.ManagerDecidedAtUtc = when;
        }
        else
        {
            entity.SecurityApproverId = approverId;
            entity.SecurityDecision = decision;
            entity.SecurityDecidedAtUtc = when;
        }
    }

    private async Task<string> NextNumberAsync(string prefix, Task<int> countTask)
    {
        var count = await countTask;
        return $"{prefix}-{DateTime.UtcNow.Year}-{(count + 1).ToString().PadLeft(4, '0')}";
    }

    private async Task<Asset> RequireAssetAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty)
            throw new DomainRuleException("Asset id is required.");
        return await db.Assets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
    }

    private async Task<Supplier> RequireSupplierAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty)
            throw new DomainRuleException("Provider id is required.");
        return await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Supplier was not found.");
    }

    private async Task RequireOptionalSupplierAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty) return;
        _ = await db.Suppliers.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ? true
            : throw new NotFoundException("Supplier was not found.");
    }

    private async Task RequireEmployeeAsync(Guid? id, string field, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty)
            throw new DomainRuleException($"{field} id is required.");
        _ = await db.Employees.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken)
            ? true
            : throw new NotFoundException($"{field} was not found.");
    }

    private async Task<AssetExpense> FindExpenseAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetExpenses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Expense was not found.");

    private async Task<Warranty> FindWarrantyAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Warranties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Warranty was not found.");

    private async Task<Warranty> ResolveClaimableWarrantyAsync(
        Guid assetId, Guid? warrantyId, CancellationToken cancellationToken)
    {
        Warranty? warranty;
        if (warrantyId.HasValue && warrantyId != Guid.Empty)
        {
            warranty = await FindWarrantyAsync(warrantyId.Value, cancellationToken);
            if (warranty.AssetId != assetId)
                throw new DomainRuleException("The warranty does not belong to this asset.");
        }
        else
        {
            warranty = await db.Warranties
                .Where(x => x.AssetId == assetId && x.State != WarrantyStates.Void)
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new DomainRuleException("No warranty is recorded for this asset.");
        }

        RefreshWarranty(warranty);
        AssetDataRules.EnsureWarrantyIsClaimable(warranty.State);
        return warranty;
    }

    private async Task<WarrantyClaim> FindClaimAsync(Guid id, CancellationToken cancellationToken) =>
        await db.WarrantyClaims.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Warranty claim was not found.");

    private async Task<DepreciationSchedule> FindScheduleAsync(Guid id, CancellationToken cancellationToken) =>
        await db.DepreciationSchedules.Include(x => x.Entries)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Depreciation schedule was not found.");

    private async Task<MaintenancePlan> FindPlanAsync(Guid id, CancellationToken cancellationToken) =>
        await db.MaintenancePlans.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Maintenance plan was not found.");

    private async Task<MaintenanceRequest> FindMaintRequestAsync(Guid id, CancellationToken cancellationToken) =>
        await db.MaintenanceRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Maintenance request was not found.");

    private async Task<WorkOrder> FindWorkOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await db.WorkOrders.Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Work order was not found.");

    private async Task<AssetInspection> FindInspectionAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetInspections.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Inspection was not found.");

    private async Task<AssetPosition> FindPositionAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AssetPositions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Asset position was not found.");

    private async Task<ExitAuthorization> FindExitAsync(Guid id, CancellationToken cancellationToken) =>
        await db.ExitAuthorizations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Exit authorization was not found.");

    private async Task<IReadOnlyCollection<OperationHistoryDto>> HistoryAsync(
        string entityType, Guid id, CancellationToken cancellationToken) =>
        await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new OperationHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);

    private static async Task<PagedResult<T>> PageAsync<T>(
        IQueryable<T> source, AssetChildListQuery query, CancellationToken cancellationToken)
    {
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(rows, query.PageNumber, query.PageSize, total, pages,
            query.PageNumber > 1, query.PageNumber < pages);
    }

    private static AssetExpenseDetailDto MapExpense(AssetExpense x) =>
        new(x.Id, x.AssetId, x.ExpenseKind, x.Amount, x.Currency, x.ExpenseDate, x.SupplierId,
            x.InvoiceNumber, x.Capitalized, x.CostCenter, x.WorkOrderId, x.DocumentId, x.Notes,
            x.State, "Draft → Recorded → Reversed");

    private static WarrantyDetailDto MapWarranty(Warranty x)
    {
        var days = (int)Math.Ceiling((x.EndDate.Date - DateTime.UtcNow.Date).TotalDays);
        var state = x.State == WarrantyStates.Void
            ? WarrantyStates.Void
            : x.EndDate.Date < DateTime.UtcNow.Date
                ? WarrantyStates.Expired
                : x.EndDate.Date <= DateTime.UtcNow.Date.AddDays(90)
                    ? WarrantyStates.Expiring
                    : WarrantyStates.Active;
        return new(x.Id, x.AssetId, x.WarrantyKind, x.ProviderId, x.Provider, x.ReferenceNumber,
            x.StartDate, x.EndDate, x.Coverage, x.Exclusions, x.ResponseTime, x.Cost, x.DocumentId,
            state, days, "Active → Expiring → Expired | Void");
    }

    private static WarrantyClaimDetailDto MapClaim(WarrantyClaim x) =>
        new(x.Id, x.ClaimNumber, x.WarrantyId, x.AssetId, x.RaisedOn, x.RaisedById, x.FaultDescription,
            x.WorkOrderId, x.ProviderReference, x.AmountClaimed, x.AmountRecovered, x.Resolution,
            x.State, x.ClosedOn, "Draft → Submitted → Acknowledged → Approved | Rejected → Settled");

    private static DepreciationScheduleDetailDto MapSchedule(DepreciationSchedule x) =>
        new(x.Id, x.AssetId, x.Method, x.AcquisitionValue, x.ResidualValue, x.UsefulLifeMonths,
            x.StartDate, x.Rate, x.PeriodLength, x.AccumulatedDepreciation, x.NetBookValue, x.State,
            x.SupersededById, CurrentCharge(x),
            x.Entries.OrderBy(e => e.PeriodStart).Select(e => new DepreciationEntryDto(
                e.Id, e.Period, e.PeriodStart, e.PeriodEnd, e.OpeningValue, e.Charge, e.ClosingValue, e.State))
            .ToArray(),
            "Draft → Running → Suspended → Completed | Superseded");

    private static MaintenancePlanDto MapPlan(MaintenancePlan x) =>
        new(x.Id, x.Code, x.Name, x.ScopeKind, x.ScopeId, x.TriggerKind, x.TaskList, x.IntervalMonths, x.IsActive);

    private static MaintenanceRequestDetailDto MapMaintRequest(MaintenanceRequest x) =>
        new(x.Id, x.RequestNumber, x.AssetId, x.RaisedById, x.RaisedOnUtc, x.FaultDescription, x.Urgency,
            x.AssetUsable, x.PhotographId, x.LocationAtReport, x.State, x.TriagedById, x.RejectionReason,
            x.WorkOrderId, "Submitted → Triaged → Accepted → Converted | Rejected");

    private static WorkOrderDetailDto MapWorkOrder(WorkOrder x) =>
        new(x.Id, x.WorkOrderNumber, x.AssetId, x.WorkKind, x.Source, x.SourceRequestId, x.Priority,
            x.ScheduledStart, x.DueDate, x.AssignedToId, x.ProviderId, x.UnderWarranty, x.AssetOutOfService,
            x.StartedAtUtc, x.CompletedAtUtc, x.DowntimeHours, x.WorkDone, x.TotalCost, x.State,
            x.Lines.OrderBy(l => l.LineNumber).Select(l => new WorkOrderLineDto(
                l.Id, l.LineNumber, l.LineKind, l.Description, l.TechnicianId, l.Hours, l.Quantity,
                l.UnitCost, l.LineCost, l.Chargeable)).ToArray(),
            "Draft → Scheduled → In progress → Completed → Verified | Canceled");

    private static AssetInspectionDetailDto MapInspection(AssetInspection x) =>
        new(x.Id, x.AssetId, x.InspectionKind, x.InspectedOn, x.InspectorId, x.Condition,
            x.LocationConfirmed, x.CustodianConfirmed, x.Findings, x.PhotographId, x.ActionRequired,
            x.WorkOrderId, x.NextDue, "Draft → Recorded → Superseded");

    private static AssetPositionDto MapPosition(AssetPosition x) =>
        new(x.Id, x.AssetId, x.FloorPlan, x.Room, x.X, x.Y, x.PositionSource, x.Confidence,
            x.RecordedAtUtc, x.RecordedBy, x.DerivedFromReader, x.Valid, x.IsCurrent);

    private static ExitAuthorizationDetailDto MapExit(ExitAuthorization x) =>
        new(x.Id, x.Number, x.Status, x.AssetId, x.RequestedById, x.RequestedAtUtc, x.Reason, x.Destination,
            x.ExpectedReturn, x.CarrierId, x.GateScope, x.ValidFromUtc, x.ValidToUtc, x.UsedAtUtc,
            x.ReturnedAtUtc, x.OwnerApproverId, x.OwnerDecision, x.OwnerDecidedAtUtc,
            x.ManagerApproverId, x.ManagerDecision, x.ManagerDecidedAtUtc,
            x.SecurityApproverId, x.SecurityDecision, x.SecurityDecidedAtUtc,
            "Draft → Pending Owner → Pending Manager → Pending Security → Approved → Active → Used");

    private void Track(string entityType, Guid entityId, string field, string? oldValue, string? newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) return;
        AddHistory(entityType, entityId, string.IsNullOrWhiteSpace(oldValue)
            ? $"{field} set to '{newValue}'"
            : $"{field} changed from '{oldValue}' to '{newValue}'");
    }

    private void AddHistory(string entityType, Guid entityId, string change) =>
        db.ChangeHistory.Add(new ChangeHistoryEntry
        {
            EntityType = entityType, EntityId = entityId, WhenUtc = DateTime.UtcNow,
            Change = change, By = currentUser.DisplayName, Source = Source
        });

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class AssetSpecification
{
    public static readonly AssetSpecificationDto Sbo048 = new(
        "SBO-048",
        "Asset",
        "The canonical enterprise physical asset. The single most consumed object in the platform.",
        "DOM-AST",
        "System identifier. Asset Number unique within Organization where used. RFID Tag and Barcode are each globally unique when assigned.",
        "Draft → Active → In Maintenance → Suspended → Disposed → Archived. Status values are configurable; transitions are configurable per Asset Type.",
        [
            new("Name", "Required", "Text", "Required (EP-001)."),
            new("Alternate Name", "Optional", "Text", "Secondary language name."),
            new("Asset Type", "Required", "Reference", "Required (EP-001)."),
            new("Status", "Required", "Reference", "Required (EP-001). Asset Status."),
            new("Asset Number", "Recommended", "Code", "Generated by the numbering scheme where configured."),
            new("Asset Category", "Recommended", "Reference", null),
            new("Asset Model", "Optional", "Reference", null),
            new("Manufacturer", "Optional", "Reference", "Derivable from Asset Model where set."),
            new("Serial Number", "Recommended", "Text", "Unique per Manufacturer where enforced by Asset Type."),
            new("Owning Organization", "Recommended", "Reference", null),
            new("Owning Department", "Recommended", "Reference", "Organizational ownership — stable."),
            new("Cost Center", "Optional", "Reference", null),
            new("Current Custodian", "Optional", "Reference", "Derived from the active Custody Assignment."),
            new("Current Location", "Recommended", "Polymorphic Ref", "Building, Room, Zone, Warehouse or Storage Location."),
            new("Location Updated At", "Optional", "Timestamp", null),
            new("Location Source", "Optional", "Enum", "Manual, Inventory, Reader, Operation."),
            new("RFID Tag", "CapabilityDependent", "Reference", "Present when RFID is enabled."),
            new("Barcode", "CapabilityDependent", "Reference", "Present when Barcode is enabled."),
            new("Purchase Date", "Optional", "Date", null),
            new("Purchase Value", "Optional", "Money", null),
            new("Supplier", "Optional", "Text", null),
            new("Purchase Reference", "Optional", "Text", null),
            new("Warranty Expiry", "Optional", "Date", "Drives warranty expiry reporting."),
            new("Depreciation Method", "CapabilityDependent", "Lookup", "Present when depreciation is enabled."),
            new("Useful Life", "CapabilityDependent", "Integer", "Months. Present when depreciation is enabled."),
            new("Residual Value", "CapabilityDependent", "Money", "Present when depreciation is enabled."),
            new("Criticality", "Optional", "Lookup", null),
            new("Parent Asset", "Optional", "Reference", "Convenience denormalization of the primary contains relationship."),
            new("Primary Image", "Optional", "Reference", null),
            new("Commissioned Date", "Optional", "Date", null),
            new("Disposal Date", "Optional", "Date", null),
            new("Disposal Reason", "Optional", "Lookup", null),
            new("Custom Attributes", "Optional", "Structure", "Defined per Asset Type.")
        ],
        [
            "Classified by Asset Type and Asset Category",
            "Identified by RFID Tag and Barcode",
            "Held under Custody Assignment",
            "Located at a Location object",
            "Related to other assets via Asset Relationship",
            "Counted by Inventory Count Line",
            "Authorized by Exit Authorization",
            "Positioned by Asset Position"
        ],
        ["BR-060", "BR-061", "BR-062", "BR-063", "BR-064", "BR-065"],
        "Full and permanent. Asset history is immutable; location and status history are retained for the life of the asset plus the retention period.",
        "Smart Warehouse: Storage Bin, Pallet, Receiving Date, Picking Priority. Inventory: Last Counted Date, Last Count Session, Verification State. Visual Intelligence references Asset Position rather than extending Asset.",
        "P1");
}
