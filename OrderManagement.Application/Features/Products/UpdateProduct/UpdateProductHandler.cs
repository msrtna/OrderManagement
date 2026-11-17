using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdateProductRequest> _validator;

    public UpdateProductHandler(
        IApplicationDbContext context,
        IValidator<UpdateProductRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<UpdateProductResult> HandleAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new UpdateProductError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new UpdateProductResult(
                false,
                errors);
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(
                product => product.Id == id && product.IsActive,
                cancellationToken);

        if (product is null)
        {
            return new UpdateProductResult(
                false,
                new[]
                {
                    new UpdateProductError(
                        "ProductNotFound",
                        "The specified product does not exist or is inactive.")
                });
        }

        var categoryExists = await _context.Categories
            .AnyAsync(
                category => category.Id == request.CategoryId &&
                            category.IsActive,
                cancellationToken);

        if (!categoryExists)
        {
            return new UpdateProductResult(
                false,
                new[]
                {
                    new UpdateProductError(
                        "CategoryNotFoundOrInactive",
                        "The specified category does not exist or is inactive.")
                });
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateProductResult(
            true,
            Array.Empty<UpdateProductError>());
    }
}