using FluentValidation;

namespace AssetsManagement.Application;

public sealed class SupplierListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? SupplierKind { get; init; }
    public string? Country { get; init; }
    public string? Rating { get; init; }
}

public sealed class SupplierRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? SupplierKind { get; init; }
    public string? ContactPerson { get; init; }
    public string? Telephone { get; init; }
    public string? Email { get; init; }
    public string? Country { get; init; }
    public string? Active { get; init; }
    public string? MoreInformation { get; init; }
    public string? AlternateName { get; init; }
    public string? TaxRegistration { get; init; }
    public string? Address { get; init; }
    public string? PaymentTerms { get; init; }
    public string? Rating { get; init; }
    public string? ExternalIdentifier { get; init; }
}

public sealed record SupplierListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? SupplierKind,
    string? ContactPerson,
    string? Telephone,
    string? Email);

public sealed record SupplierDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? SupplierKind,
    string? ContactPerson,
    string? Telephone,
    string? Email,
    string? Country,
    string Active,
    string? AlternateName,
    string? TaxRegistration,
    string? Address,
    string? PaymentTerms,
    string? Rating,
    string? ExternalIdentifier,
    string Lifecycle);

public sealed record SupplierHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface ISupplierService
{
    Task<PagedResult<SupplierListItemDto>> ListAsync(SupplierListQuery query, CancellationToken cancellationToken);
    Task<SupplierDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplierDetailDto> CreateAsync(SupplierRequest request, CancellationToken cancellationToken);
    Task<SupplierDetailDto> UpdateAsync(Guid id, SupplierRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplierDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SupplierHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken);
}

public sealed class SupplierListQueryValidator : AbstractValidator<SupplierListQuery>
{
    public SupplierListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class SupplierRequestValidator : AbstractValidator<SupplierRequest>
{
    public static readonly string[] Kinds = ["Vendor", "Service provider", "Both"];
    public static readonly string[] Ratings = ["Preferred", "Approved", "Under review", "Blocked"];

    public SupplierRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Active).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Active must be Yes or No.");
        RuleFor(x => x.MoreInformation).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("More information must be Yes or No.");
        RuleFor(x => x.SupplierKind).Must(x => Kinds.Contains(x!))
            .When(x => !string.IsNullOrWhiteSpace(x.SupplierKind))
            .WithMessage("Supplier kind must be Vendor, Service provider or Both.");
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Telephone).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(254).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.AlternateName).MaximumLength(200);
        When(x => YesNoParser.TryParse(x.MoreInformation) == true, () =>
        {
            RuleFor(x => x.TaxRegistration).MaximumLength(100);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.PaymentTerms).MaximumLength(100);
            RuleFor(x => x.Rating).Must(x => Ratings.Contains(x!))
                .When(x => !string.IsNullOrWhiteSpace(x.Rating))
                .WithMessage("Rating must be Preferred, Approved, Under review or Blocked.");
            RuleFor(x => x.ExternalIdentifier).MaximumLength(100);
        });
    }
}
