using FluentValidation;

namespace AssetsManagement.Application;

public sealed class EmployeeListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? Active { get; init; }
    public string? Department { get; init; }
}

public sealed class EmployeeRequest
{
    public string Name { get; init; } = "";
    public string? Code { get; init; }
    public string? Email { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public bool? Active { get; init; }
}

public sealed record EmployeeListItemDto(
    Guid Id,
    string Name,
    string Code,
    string? Department,
    bool Active);

public sealed record EmployeeDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? Email,
    string? Department,
    string? JobTitle,
    bool Active);

public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken);
    Task<EmployeeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<EmployeeDetailDto> CreateAsync(EmployeeRequest request, CancellationToken cancellationToken);
    Task<EmployeeDetailDto> UpdateAsync(Guid id, EmployeeRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<EmployeeDetailDto> RestoreAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> LookupAsync(string? search, CancellationToken cancellationToken);
}

public sealed class EmployeeListQueryValidator : AbstractValidator<EmployeeListQuery>
{
    public EmployeeListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}

public sealed class EmployeeRequestValidator : AbstractValidator<EmployeeRequest>
{
    public EmployeeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(254).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Department).MaximumLength(200);
        RuleFor(x => x.JobTitle).MaximumLength(200);
        RuleFor(x => x.Active).NotNull().WithMessage("Active is required.");
    }
}
