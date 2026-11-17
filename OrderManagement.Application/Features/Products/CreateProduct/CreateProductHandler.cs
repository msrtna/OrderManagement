using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateProductRequest> _validator;

    public CreateProductHandler(
        IApplicationDbContext context,
        IValidator<CreateProductRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<CreateProductResult> HandleAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new CreateProductError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new CreateProductResult(
                false,
                null,
                errors);
        }

        var categoryExists = await _context.Categories
            .AnyAsync(
                category => category.Id == request.CategoryId &&
                            category.IsActive,
                cancellationToken);

        if (!categoryExists)
        {
            return new CreateProductResult(
                false,
                null,
                new[]
                {
                    new CreateProductError(
                        "CategoryNotFoundOrInactive",
                        "The specified category does not exist or is inactive.")
                });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            IsActive = true
        };

        await _context.Products.AddAsync(
            product,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CreateProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId,
            product.IsActive);

        return new CreateProductResult(
            true,
            response,
            Array.Empty<CreateProductError>());
    }
}