using FluentValidation;

namespace AssetsManagement.Application;

public sealed class ManufacturerListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
}

public sealed class ManufacturerRequest
{
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string? Active { get; init; }
    public string? MoreInformation { get; init; }
    public string? AlternateName { get; init; }
    public string? Country { get; init; }
    public string? SupportContact { get; init; }
    public string? Website { get; init; }
}

public sealed record ManufacturerListItemDto(Guid Id, string Name, string Code, bool Active);

public sealed record ManufacturerDetailDto(
    Guid Id,
    string Name,
    string Code,
    string Active,
    string? AlternateName,
    string? Country,
    string? SupportContact,
    string? Website,
    string Lifecycle);

public sealed record ManufacturerHistoryDto(
    DateTime When,
    string Change,
    string By,
    string Source);

public interface IManufacturerService
{
    Task<PagedResult<ManufacturerListItemDto>> ListAsync(ManufacturerListQuery query, CancellationToken cancellationToken);
    Task<ManufacturerDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ManufacturerDetailDto> CreateAsync(ManufacturerRequest request, CancellationToken cancellationToken);
    Task<ManufacturerDetailDto> UpdateAsync(Guid id, ManufacturerRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<ManufacturerDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ManufacturerHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}

public static class YesNoParser
{
    public static bool? TryParse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "yes" or "true" => true,
        "no" or "false" => false,
        _ => null
    };

    public static bool IsYesNo(string? value) => TryParse(value).HasValue;
    public static string Format(bool value) => value ? "Yes" : "No";
}

public sealed class ManufacturerListQueryValidator : AbstractValidator<ManufacturerListQuery>
{
    public ManufacturerListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class ManufacturerRequestValidator : AbstractValidator<ManufacturerRequest>
{
    public ManufacturerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$");
        RuleFor(x => x.Active).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("Active must be Yes or No.");
        RuleFor(x => x.MoreInformation).NotEmpty().Must(YesNoParser.IsYesNo)
            .WithMessage("More information must be Yes or No.");
        RuleFor(x => x.AlternateName).MaximumLength(200);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.SupportContact).MaximumLength(1000);
        RuleFor(x => x.Website).MaximumLength(500)
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Website) && YesNoParser.TryParse(x.MoreInformation) == true)
            .WithMessage("Website must be a valid absolute URL.");
    }
}
