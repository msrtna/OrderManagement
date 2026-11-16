using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Products.DeactivateProduct;

public sealed class DeactivateProductHandler
{
    private readonly IApplicationDbContext _context;

    public DeactivateProductHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeactivateProductResult> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(
                product => product.Id == id && product.IsActive,
                cancellationToken);

        if (product is null)
        {
            return new DeactivateProductResult(
                false,
                new[]
                {
                    new DeactivateProductError(
                        "ProductNotFound",
                        "The specified product does not exist or is already inactive.")
                });
        }

        product.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);

        return new DeactivateProductResult(
            true,
            Array.Empty<DeactivateProductError>());
    }
}