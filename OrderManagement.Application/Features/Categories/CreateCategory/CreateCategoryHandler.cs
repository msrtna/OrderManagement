
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateCategoryRequest> _validator;

    public CreateCategoryHandler(
        IApplicationDbContext context,
        IValidator<CreateCategoryRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<CreateCategoryResult> HandleAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new CreateCategoryError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new CreateCategoryResult(
                false,
                null,
                errors);
        }

        var categoryName = request.Name.Trim();

        var categoryExists = await _context.Categories
            .AnyAsync(
                category => category.Name == categoryName,
                cancellationToken);

        if (categoryExists)
        {
            return new CreateCategoryResult(
                false,
                null,
                new[]
                {
                    new CreateCategoryError(
                        "DuplicateCategoryName",
                        "A category with the specified name already exists.")
                });
        }

        var category = new Category
        {
            Name = categoryName,
            Description = request.Description?.Trim(),
            IsActive = true
        };

        await _context.Categories.AddAsync(
            category,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CreateCategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive);

        return new CreateCategoryResult(
            true,
            response,
            Array.Empty<CreateCategoryError>());
    }
}
