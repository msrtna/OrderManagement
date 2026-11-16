using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Products.GetProductById;

public sealed class GetProductByIdHandler
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetProductByIdResponse?> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(product =>
                product.Id == id &&
                product.IsActive)
            .Select(product => new GetProductByIdResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CategoryId,
                product.Category.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }
}