using FluentValidation;

namespace AssetsManagement.Application;

public sealed class BarcodeListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public string? Symbology { get; init; }
    public string? Status { get; init; }
    public string? SubjectReference { get; init; }
}

public sealed class BarcodeRequest
{
    public string Value { get; init; } = "";
    public string Symbology { get; init; } = "";
    public string? Status { get; init; }
    public string? SubjectReference { get; init; }
}

public sealed record BarcodeListItemDto(
    Guid Id,
    string Value,
    string Symbology,
    string Status);

public sealed record BarcodeDetailDto(
    Guid Id,
    string Value,
    string Symbology,
    string Status,
    string? SubjectReference,
    DateTime? PrintedAt,
    string? ReplacedBy,
    string Lifecycle);

public sealed record BarcodeHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public sealed record BarcodeWaitingAssetDto(
    Guid Id,
    string Name,
    string AssetNumber,
    string? AssetType);

public interface IBarcodeService
{
    Task<PagedResult<BarcodeListItemDto>> ListAsync(BarcodeListQuery query, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> CreateAsync(BarcodeRequest request, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> UpdateAsync(Guid id, BarcodeRequest request, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> GenerateAsync(string symbology, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> AssignAsync(Guid id, Guid assetId, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> UnassignAsync(Guid id, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> ReplaceAsync(Guid id, Guid replacementId, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<BarcodeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BarcodeHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string value, string? symbology, Guid? excludingId, CancellationToken cancellationToken);
    Task<PagedResult<BarcodeWaitingAssetDto>> ListAssetsWaitingAsync(
        BarcodeListQuery query, CancellationToken cancellationToken);
}

public sealed class BarcodeListQueryValidator : AbstractValidator<BarcodeListQuery>
{
    public BarcodeListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class BarcodeRequestValidator : AbstractValidator<BarcodeRequest>
{
    public static readonly string[] Symbologies = ["Code128", "Code39", "QR", "DataMatrix", "EAN"];
    public static readonly string[] Statuses = ["Unassigned", "Assigned", "Replaced", "Retired"];

    public BarcodeRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(128)
            .Matches("^[A-Za-z0-9._-]+$");
        RuleFor(x => x.Symbology).NotEmpty().Must(x => Symbologies.Contains(x))
            .WithMessage("Supported symbologies are Code128, Code39, QR, DataMatrix and EAN.");
        RuleFor(x => x.Status).NotEmpty().Must(x => Statuses.Contains(x!))
            .WithMessage("Status must be Unassigned, Assigned, Replaced or Retired.");
        RuleFor(x => x.SubjectReference).NotEmpty()
            .When(x => string.Equals(x.Status, "Assigned", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Subject reference is required when the barcode is assigned.");
        RuleFor(x => x.SubjectReference).Empty()
            .When(x => string.Equals(x.Status, "Unassigned", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("An unassigned barcode cannot be linked to an asset.");
        RuleFor(x => x.SubjectReference).MaximumLength(200);
    }
}
