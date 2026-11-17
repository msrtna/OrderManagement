
using FluentValidation;

namespace OrderManagement.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryValidator
    : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Category ID must be greater than zero.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required.")
            .MaximumLength(150)
            .WithMessage("Category name cannot exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Category description cannot exceed 500 characters.");
    }
}
