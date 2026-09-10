using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

public sealed class AssetDocumentService(
    AssetsDbContext db,
    IFileStorageService files,
    ICurrentUser currentUser) : IAssetDocumentService
{
    private const string EntityType = nameof(AssetDocument);
    private const string Lifecycle = "Uploaded → Current → Superseded | Expired → Archived";
    private const string Source = "Screen";
    private const string Folder = "asset-documents";

    public async Task<PagedResult<AssetDocumentListItemDto>> ListAsync(
        AssetDocumentListQuery query, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow.Date;
        var source = db.AssetDocuments.AsNoTracking().Include(x => x.Links).AsQueryable();
        if (query.AssetId.HasValue)
            source = source.Where(x => x.Links.Any(l => l.AssetId == query.AssetId));
        if (!string.IsNullOrWhiteSpace(query.DocumentKind))
            source = source.Where(x => x.DocumentKind == query.DocumentKind);
        if (!string.IsNullOrWhiteSpace(query.State))
            source = source.Where(x => x.State == query.State);
        if (query.Confidential.HasValue)
            source = source.Where(x => x.Confidential == query.Confidential.Value);
        if (query.Expired == true)
            source = source.Where(x => x.ExpiresOn != null && x.ExpiresOn < now);
        if (query.ExpiringWithinDays.HasValue)
        {
            var until = now.AddDays(query.ExpiringWithinDays.Value);
            source = source.Where(x => x.ExpiresOn != null && x.ExpiresOn >= now && x.ExpiresOn <= until);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Title.Contains(search) ||
                (x.IssuedBy != null && x.IssuedBy.Contains(search)) ||
                (x.ReferenceNumber != null && x.ReferenceNumber.Contains(search)) ||
                x.OriginalFileName.Contains(search) ||
                x.Links.Any(l => l.Asset.Name.Contains(search) || l.Asset.AssetNumber.Contains(search)));
        }
        source = (query.SortBy?.ToLowerInvariant(), query.SortDirection) switch
        {
            ("title", "desc") => source.OrderByDescending(x => x.Title),
            ("title", _) => source.OrderBy(x => x.Title),
            ("documentkind", "desc") => source.OrderByDescending(x => x.DocumentKind),
            ("documentkind", _) => source.OrderBy(x => x.DocumentKind),
            ("expireson", "desc") => source.OrderByDescending(x => x.ExpiresOn),
            ("expireson", _) => source.OrderBy(x => x.ExpiresOn),
            (_, "asc") => source.OrderBy(x => x.DocumentDate),
            _ => source.OrderByDescending(x => x.CreatedAtUtc)
        };
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AssetDocumentListItemDto(
                x.Id, x.Title, x.DocumentKind, x.Links.Select(l => l.AssetId).ToArray(), x.Amount,
                x.DocumentDate, x.ExpiresOn, x.IssuedBy, x.ReferenceNumber, x.OriginalFileName,
                x.Confidential, EffectiveState(x.State, x.ExpiresOn, now)))
            .ToArrayAsync(cancellationToken);
        return Page(rows, query.PageNumber, query.PageSize, total);
    }

    public async Task<AssetDocumentDetailDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await FindAsync(id, cancellationToken, tracking: false));

    public async Task<AssetDocumentDetailDto> CreateAsync(
        AssetDocumentCreateRequest request, DocumentUpload upload, CancellationToken cancellationToken)
    {
        var assetIds = await RequireAssetsAsync(request.AssetIds, cancellationToken);
        var stored = await files.SaveAsync(
            upload.Content, Path.GetExtension(upload.OriginalFileName), Folder, cancellationToken);
        var entity = new AssetDocument
        {
            DocumentKind = request.DocumentKind!,
            Title = request.Title!.Trim(),
            StoredFileName = stored,
            OriginalFileName = Path.GetFileName(upload.OriginalFileName),
            ContentType = upload.ContentType,
            SizeBytes = upload.SizeBytes,
            DocumentDate = request.DocumentDate ?? DateTime.UtcNow.Date,
            ExpiresOn = request.ExpiresOn,
            IssuedBy = NullIfEmpty(request.IssuedBy),
            ReferenceNumber = NullIfEmpty(request.ReferenceNumber),
            Confidential = request.Confidential == true,
            Version = 1,
            State = DocumentStates.Current,
            Amount = request.Amount,
            UploadedBy = currentUser.UserName
        };
        AttachLinks(entity, assetIds, request.Allocations);
        db.AssetDocuments.Add(entity);
        AddHistory(entity.Id, "Record created");
        AddHistory(entity.Id, $"Title set to '{entity.Title}'");
        AddHistory(entity.Id, $"File set to '{entity.OriginalFileName}'");
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AssetDocumentDetailDto> UpdateAsync(
        Guid id, AssetDocumentRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, tracking: true);
        if (entity.State is DocumentStates.Superseded or DocumentStates.Archived)
            throw new DomainRuleException("A superseded or archived document cannot be edited.");
        var assetIds = await RequireAssetsAsync(request.AssetIds, cancellationToken);
        Track(entity.Id, "Title", entity.Title, request.Title!.Trim());
        Track(entity.Id, "Document Kind", entity.DocumentKind, request.DocumentKind);
        entity.DocumentKind = request.DocumentKind!;
        entity.Title = request.Title.Trim();
        entity.DocumentDate = request.DocumentDate ?? entity.DocumentDate;
        entity.ExpiresOn = request.ExpiresOn;
        entity.IssuedBy = NullIfEmpty(request.IssuedBy);
        entity.ReferenceNumber = NullIfEmpty(request.ReferenceNumber);
        entity.Confidential = request.Confidential == true;
        entity.Amount = request.Amount;
        db.AssetDocumentLinks.RemoveRange(entity.Links);
        AttachLinks(entity, assetIds, request.Allocations);
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AssetDocumentDetailDto> SupersedeAsync(
        Guid id, AssetDocumentCreateRequest request, DocumentUpload upload, CancellationToken cancellationToken)
    {
        var previous = await FindAsync(id, cancellationToken, tracking: true);
        if (previous.State == DocumentStates.Superseded)
            throw new DomainRuleException("That document has already been superseded.");
        var created = await CreateAsync(request, upload, cancellationToken);
        var replacement = await db.AssetDocuments.SingleAsync(x => x.Id == created.Id, cancellationToken);
        replacement.Version = previous.Version + 1;
        previous.State = DocumentStates.Superseded;
        previous.SupersededById = replacement.Id;
        AddHistory(previous.Id, $"Status changed from '{DocumentStates.Current}' to '{DocumentStates.Superseded}'");
        AddHistory(replacement.Id, $"Version set to '{replacement.Version}'");
        await db.SaveChangesAsync(cancellationToken);
        return Map(replacement);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, tracking: true);
        entity.State = DocumentStates.Archived;
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedBy = currentUser.UserName;
        AddHistory(entity.Id, "Status changed to 'Archived'");
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssetDocumentDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, tracking: true);
        entity.IsActive = true;
        entity.DeletedAtUtc = null;
        entity.DeletedBy = null;
        if (entity.State == DocumentStates.Archived)
            entity.State = DocumentStates.Current;
        AddHistory(entity.Id, "Record restored");
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyCollection<AssetDocumentHistoryDto>> GetHistoryAsync(
        Guid id, CancellationToken cancellationToken)
    {
        _ = await FindAsync(id, cancellationToken, tracking: false);
        return await db.ChangeHistory.AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == id)
            .OrderByDescending(x => x.WhenUtc)
            .Select(x => new AssetDocumentHistoryDto(x.WhenUtc, x.Change, x.By, x.Source))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken, tracking: false);
        AddHistory(entity.Id, entity.Confidential
            ? "Confidential download recorded"
            : "Download recorded");
        await db.SaveChangesAsync(cancellationToken);
        var stream = await files.OpenReadAsync(entity.StoredFileName, Folder, cancellationToken);
        return (stream, entity.ContentType, entity.OriginalFileName);
    }

    private async Task<Guid[]> RequireAssetsAsync(
        IReadOnlyCollection<Guid>? assetIds, CancellationToken cancellationToken)
    {
        var ids = (assetIds ?? []).Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            throw new DomainRuleException("At least one asset id is required.");
        var count = await db.Assets.CountAsync(x => ids.Contains(x.Id), cancellationToken);
        if (count != ids.Length)
            throw new NotFoundException("One or more assets were not found.");
        return ids;
    }

    private static void AttachLinks(
        AssetDocument entity, IReadOnlyCollection<Guid> assetIds, IReadOnlyDictionary<Guid, decimal>? allocations)
    {
        foreach (var assetId in assetIds)
        {
            decimal? amount = null;
            if (allocations is not null && allocations.TryGetValue(assetId, out var allocated))
                amount = allocated;
            entity.Links.Add(new AssetDocumentLink { AssetId = assetId, AllocatedAmount = amount });
        }
    }

    private async Task<AssetDocument> FindAsync(Guid id, CancellationToken cancellationToken, bool tracking)
    {
        var source = tracking ? db.AssetDocuments.Include(x => x.Links) : db.AssetDocuments.AsNoTracking().Include(x => x.Links);
        return await source.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Asset document was not found.");
    }

    private static AssetDocumentDetailDto Map(AssetDocument entity) =>
        new(entity.Id, entity.Title, entity.DocumentKind,
            entity.Links.Select(x => new AssetDocumentLinkDto(x.AssetId, x.AllocatedAmount)).ToArray(),
            entity.Amount, entity.DocumentDate, entity.ExpiresOn, entity.IssuedBy, entity.ReferenceNumber,
            entity.OriginalFileName, entity.ContentType, entity.SizeBytes, entity.Confidential, entity.Version,
            EffectiveState(entity.State, entity.ExpiresOn, DateTime.UtcNow.Date), entity.SupersededById,
            entity.UploadedBy, $"/api/asset-documents/{entity.Id}/content", Lifecycle);

    private static string EffectiveState(string state, DateTime? expiresOn, DateTime today) =>
        state is DocumentStates.Current or DocumentStates.Uploaded && expiresOn < today
            ? DocumentStates.Expired : state;

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> rows, int page, int size, int total)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)size);
        return new(rows, page, size, total, pages, page > 1, page < pages);
    }

    private void Track(Guid entityId, string field, string? oldValue, string? newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) return;
        AddHistory(entityId, $"{field} changed from '{oldValue}' to '{newValue}'");
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
