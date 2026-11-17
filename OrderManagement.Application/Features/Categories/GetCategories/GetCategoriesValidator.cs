
using FluentValidation;

namespace OrderManagement.Application.Features.Categories.GetCategories;

public sealed class GetCategoriesValidator
    : AbstractValidator<GetCategoriesRequest>
{
    public GetCategoriesValidator()
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
    }
}
