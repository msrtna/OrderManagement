using FluentValidation;

namespace OrderManagement.Application.Features.Products.GetProducts;

public sealed class GetProductsValidator
    : AbstractValidator<GetProductsRequest>
{
    public GetProductsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Search term cannot exceed 100 characters.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("Category ID must be greater than zero.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("Minimum price cannot be negative.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("Maximum price cannot be negative.");

        RuleFor(x => x)
            .Must(x =>
                !x.MinPrice.HasValue ||
                !x.MaxPrice.HasValue ||
                x.MinPrice <= x.MaxPrice)
            .WithMessage("Minimum price cannot exceed maximum price.");

        RuleFor(x => x.SortBy)
            .Must(sortBy =>
                sortBy.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                sortBy.Equals("Price", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort by must be either Name or Price.");
    }
}