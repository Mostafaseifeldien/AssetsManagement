using AssetsManagement.Domain;
using FluentValidation;

namespace AssetsManagement.Application;

public sealed class AssetDocumentListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public Guid? AssetId { get; init; }
    public string? DocumentKind { get; init; }
    public string? State { get; init; }
    public bool? Confidential { get; init; }
    public int? ExpiringWithinDays { get; init; }
    public bool? Expired { get; init; }
}

public sealed class AssetDocumentCreateRequest
{
    public IReadOnlyCollection<Guid>? AssetIds { get; init; }
    public string? DocumentKind { get; init; }
    public string? Title { get; init; }
    public DateTime? DocumentDate { get; init; }
    public DateTime? ExpiresOn { get; init; }
    public string? IssuedBy { get; init; }
    public string? ReferenceNumber { get; init; }
    public bool? Confidential { get; init; }
    public decimal? Amount { get; init; }
    public IReadOnlyDictionary<Guid, decimal>? Allocations { get; init; }
}

public sealed class AssetDocumentRequest
{
    public IReadOnlyCollection<Guid>? AssetIds { get; init; }
    public string? DocumentKind { get; init; }
    public string? Title { get; init; }
    public DateTime? DocumentDate { get; init; }
    public DateTime? ExpiresOn { get; init; }
    public string? IssuedBy { get; init; }
    public string? ReferenceNumber { get; init; }
    public bool? Confidential { get; init; }
    public decimal? Amount { get; init; }
    public IReadOnlyDictionary<Guid, decimal>? Allocations { get; init; }
}

public sealed class DocumentUpload
{
    public required Stream Content { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
}

public sealed record AssetDocumentLinkDto(Guid AssetId, decimal? AllocatedAmount);

public sealed record AssetDocumentListItemDto(
    Guid Id,
    string Title,
    string DocumentKind,
    IReadOnlyCollection<Guid> AssetIds,
    decimal? Amount,
    DateTime? DocumentDate,
    DateTime? ExpiresOn,
    string? IssuedBy,
    string? ReferenceNumber,
    string File,
    bool Confidential,
    string State);

public sealed record AssetDocumentDetailDto(
    Guid Id,
    string Title,
    string DocumentKind,
    IReadOnlyCollection<AssetDocumentLinkDto> Assets,
    decimal? Amount,
    DateTime? DocumentDate,
    DateTime? ExpiresOn,
    string? IssuedBy,
    string? ReferenceNumber,
    string File,
    string ContentType,
    long SizeBytes,
    bool Confidential,
    int Version,
    string State,
    Guid? SupersededById,
    string UploadedBy,
    string ContentUrl,
    string Lifecycle);

public sealed record AssetDocumentHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IAssetDocumentService
{
    Task<PagedResult<AssetDocumentListItemDto>> ListAsync(
        AssetDocumentListQuery query, CancellationToken cancellationToken);
    Task<AssetDocumentDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetDocumentDetailDto> CreateAsync(
        AssetDocumentCreateRequest request, DocumentUpload upload, CancellationToken cancellationToken);
    Task<AssetDocumentDetailDto> UpdateAsync(Guid id, AssetDocumentRequest request, CancellationToken cancellationToken);
    Task<AssetDocumentDetailDto> SupersedeAsync(
        Guid id, AssetDocumentCreateRequest request, DocumentUpload upload, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AssetDocumentDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssetDocumentHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType, string FileName)> OpenAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AssetDocumentListQueryValidator : AbstractValidator<AssetDocumentListQuery>
{
    public AssetDocumentListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
        RuleFor(x => x.DocumentKind).Must(x => x is null || DocumentKinds.All.Contains(x))
            .WithMessage("Document kind is not recognised.");
        RuleFor(x => x.State).Must(x => x is null || DocumentStates.All.Contains(x))
            .WithMessage("State is not recognised.");
        RuleFor(x => x.ExpiringWithinDays).InclusiveBetween(1, 3650).When(x => x.ExpiringWithinDays.HasValue);
    }
}

public sealed class AssetDocumentCreateRequestValidator : AbstractValidator<AssetDocumentCreateRequest>
{
    public AssetDocumentCreateRequestValidator()
    {
        RuleFor(x => x.AssetIds).NotEmpty().WithMessage("At least one asset id is required.");
        RuleFor(x => x.DocumentKind).NotEmpty().Must(x => DocumentKinds.All.Contains(x!))
            .WithMessage("Document kind must be Invoice, Purchase contract, Warranty certificate, Calibration certificate, Manual, Insurance policy, Disposal record or Other.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IssuedBy).MaximumLength(200);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Confidential).NotNull().WithMessage("Confidential is required.");
    }
}

public sealed class AssetDocumentRequestValidator : AbstractValidator<AssetDocumentRequest>
{
    public AssetDocumentRequestValidator()
    {
        RuleFor(x => x.AssetIds).NotEmpty().WithMessage("At least one asset id is required.");
        RuleFor(x => x.DocumentKind).NotEmpty().Must(x => DocumentKinds.All.Contains(x!))
            .WithMessage("Document kind is not recognised.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IssuedBy).MaximumLength(200);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Confidential).NotNull().WithMessage("Confidential is required.");
    }
}
