using FluentValidation;

namespace AssetsManagement.Application;

public sealed class MasterDataRequestValidator : AbstractValidator<MasterDataRequest>
{
    public MasterDataRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AlternateName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Website).Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultUsefulLifeMonths).GreaterThan(0).When(x => x.DefaultUsefulLifeMonths.HasValue);
        RuleFor(x => x.ExpectedUsefulLifeMonths).GreaterThan(0).When(x => x.ExpectedUsefulLifeMonths.HasValue);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.Color));
        RuleFor(x => x.SupplierKind).Must(x => x is "Vendor" or "Service provider" or "Both")
            .When(x => !string.IsNullOrWhiteSpace(x.SupplierKind));
        RuleFor(x => x.Rating).Must(x => x is "Preferred" or "Approved" or "Under review" or "Blocked")
            .When(x => !string.IsNullOrWhiteSpace(x.Rating));
        RuleFor(x => x.StatusCategory).Must(x => x is "Working" or "Damaged" or "In Maintenance" or
                "Missing" or "In Transit" or "Disposed" or "Unknown")
            .When(x => !string.IsNullOrWhiteSpace(x.StatusCategory));
        RuleFor(x => x.DataType).Must(x => x is "Text" or "Number" or "Money" or "Date" or
                "YesNo" or "Yes or no" or "Yes, No" or "List" or "Reference")
            .When(x => !string.IsNullOrWhiteSpace(x.DataType));
        RuleFor(x => x.ListValues).NotEmpty()
            .When(x => string.Equals(x.DataType, "List", StringComparison.OrdinalIgnoreCase));
    }
}
