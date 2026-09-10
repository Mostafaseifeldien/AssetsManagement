using FluentValidation;

namespace AssetsManagement.Application;

public sealed class ListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDirection { get; init; } = "asc";
    public bool? IsActive { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? Status { get; init; }
    public Guid? AssetCategoryId { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? ManufacturerId { get; init; }
    public Guid? AssetTypeId { get; init; }
    public string? SupplierKind { get; init; }
    public string? Rating { get; init; }
    public string? StatusCategory { get; init; }
    public string? DataType { get; init; }
}

public sealed class ListQueryValidator : AbstractValidator<ListQuery>
{
    public ListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc")
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}
