
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdateCategoryRequest> _validator;

    public UpdateCategoryHandler(
        IApplicationDbContext context,
        IValidator<UpdateCategoryRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<UpdateCategoryResult> HandleAsync(
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new UpdateCategoryError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new UpdateCategoryResult(false, errors);
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(
                category => category.Id == request.Id &&
                            category.IsActive,
                cancellationToken);

        if (category is null)
        {
            return new UpdateCategoryResult(
                false,
                new[]
                {
                    new UpdateCategoryError(
                        "CategoryNotFound",
                        "The specified active category was not found.")
                });
        }

        var categoryName = request.Name.Trim();

        var duplicateNameExists = await _context.Categories
            .AnyAsync(
                existingCategory =>
                    existingCategory.Id != request.Id &&
                    existingCategory.Name == categoryName,
                cancellationToken);

        if (duplicateNameExists)
        {
            return new UpdateCategoryResult(
                false,
                new[]
                {
                    new UpdateCategoryError(
                        "DuplicateCategoryName",
                        "A category with the specified name already exists.")
                });
        }

        category.Name = categoryName;
        category.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateCategoryResult(
            true,
            Array.Empty<UpdateCategoryError>());
    }
}
