using AssetsManagement.Domain;
using FluentValidation;

namespace AssetsManagement.Application;

public sealed class CustodyListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public Guid? AssetId { get; init; }
    public Guid? CustodianId { get; init; }
    public string? Status { get; init; }
    public Guid? BatchId { get; init; }
}

public sealed class CustodyAssignmentRequest
{
    public Guid? AssetId { get; init; }
    public Guid? CustodianId { get; init; }
    public string? CustodianType { get; init; }
    public DateTime? AssignedFrom { get; init; }
    public string? AssignmentReason { get; init; }
    public bool? Acknowledged { get; init; }
    public string? HandoverDocument { get; init; }
    public bool? RequireAcknowledgement { get; init; }
}

public sealed class CustodyTransferRequest
{
    public IReadOnlyCollection<Guid>? AssetIds { get; init; }
    public Guid? CustodianId { get; init; }
    public string? CustodianType { get; init; }
    public string? Reason { get; init; }
    public DateTime? AssignedFrom { get; init; }
    public bool? RequireAcknowledgement { get; init; }
}

public sealed record CustodyAssignmentListItemDto(
    Guid Id,
    Guid AssetId,
    Guid CustodianId,
    string CustodianType,
    DateTime AssignedFrom,
    DateTime? AssignedTo,
    string? AssignmentReason,
    string Status);

public sealed record CustodyAssignmentDetailDto(
    Guid Id,
    Guid AssetId,
    Guid CustodianId,
    string CustodianType,
    DateTime AssignedFrom,
    DateTime? AssignedTo,
    string AssignedBy,
    string? AssignmentReason,
    bool Acknowledged,
    DateTime? AcknowledgedAt,
    string Status,
    string? HandoverDocument,
    Guid? BatchId,
    string Lifecycle);

public sealed record CustodyTransferListItemDto(
    Guid Id,
    string BatchNumber,
    Guid CustodianId,
    string Reason,
    DateTime AssignedFrom,
    int AssetCount,
    string AssignedBy);

public sealed record CustodyTransferDetailDto(
    Guid Id,
    string BatchNumber,
    Guid CustodianId,
    string CustodianType,
    string Reason,
    DateTime AssignedFrom,
    bool RequireAcknowledgement,
    string AssignedBy,
    int AssetCount,
    IReadOnlyCollection<Guid> AssetIds);

public sealed record CustodyHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface ICustodyService
{
    Task<PagedResult<CustodyAssignmentListItemDto>> ListAsync(CustodyListQuery query, CancellationToken cancellationToken);
    Task<CustodyAssignmentDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CustodyAssignmentDetailDto> CreateAsync(CustodyAssignmentRequest request, CancellationToken cancellationToken);
    Task<CustodyAssignmentDetailDto> UpdateAsync(Guid id, CustodyAssignmentRequest request, CancellationToken cancellationToken);
    Task<CustodyAssignmentDetailDto> AcknowledgeAsync(Guid id, CancellationToken cancellationToken);
    Task<CustodyAssignmentDetailDto> DisputeAsync(Guid id, CancellationToken cancellationToken);
    Task<CustodyAssignmentDetailDto> CloseAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CustodyHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<CustodyTransferListItemDto>> ListTransfersAsync(CustodyListQuery query, CancellationToken cancellationToken);
    Task<CustodyTransferDetailDto> GetTransferAsync(Guid id, CancellationToken cancellationToken);
    Task<CustodyTransferDetailDto> TransferAsync(CustodyTransferRequest request, CancellationToken cancellationToken);
}

public sealed class CustodyListQueryValidator : AbstractValidator<CustodyListQuery>
{
    public CustodyListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
        RuleFor(x => x.Status).Must(x => x is null || CustodyStatuses.All.Contains(x))
            .WithMessage("Status must be Active, Closed or Disputed.");
    }
}

public sealed class CustodyAssignmentRequestValidator : AbstractValidator<CustodyAssignmentRequest>
{
    public CustodyAssignmentRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty().WithMessage("Asset id is required.");
        RuleFor(x => x.CustodianId).NotEmpty().WithMessage("Custodian id is required.");
        RuleFor(x => x.CustodianType).NotEmpty().Must(x => CustodyTypes.All.Contains(x!))
            .WithMessage("Custodian type must be Employee, Contractor, Department or Team.");
        RuleFor(x => x.AssignmentReason).MaximumLength(200);
        RuleFor(x => x.HandoverDocument).MaximumLength(200);
    }
}

public sealed class CustodyTransferRequestValidator : AbstractValidator<CustodyTransferRequest>
{
    public CustodyTransferRequestValidator()
    {
        RuleFor(x => x.AssetIds).NotEmpty().WithMessage("At least one asset id is required.");
        RuleFor(x => x.CustodianId).NotEmpty().WithMessage("Custodian id is required.");
        RuleFor(x => x.CustodianType).NotEmpty().Must(x => CustodyTypes.All.Contains(x!))
            .WithMessage("Custodian type must be Employee, Contractor, Department or Team.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
