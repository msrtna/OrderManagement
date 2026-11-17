
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Categories.DeactivateCategory;

public sealed class DeactivateCategoryHandler
{
    private readonly IApplicationDbContext _context;

    public DeactivateCategoryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeactivateCategoryResult> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return new DeactivateCategoryResult(
                false,
                new[]
                {
                    new DeactivateCategoryError(
                        "InvalidCategoryId",
                        "Category ID must be greater than zero.")
                });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id && category.IsActive,
                cancellationToken);

        if (category is null)
        {
            return new DeactivateCategoryResult(
                false,
                new[]
                {
                    new DeactivateCategoryError(
                        "CategoryNotFound",
                        "The specified active category was not found.")
                });
        }

        category.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);

        return new DeactivateCategoryResult(
            true,
            Array.Empty<DeactivateCategoryError>());
    }
}
