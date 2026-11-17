using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Products.GetProducts;

public sealed class GetProductsHandler
{
    private readonly IApplicationDbContext _context;

    public GetProductsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetProductsResponse> HandleAsync(
        GetProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(product => product.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();

            query = query.Where(product =>
                product.Name.Contains(searchTerm));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product =>
                product.CategoryId == request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(product =>
                product.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(product =>
                product.Price <= request.MaxPrice.Value);
        }

        query = request.SortBy.ToLowerInvariant() switch
        {
            "price" when request.SortDescending =>
                query.OrderByDescending(product => product.Price)
                     .ThenBy(product => product.Id),

            "price" =>
                query.OrderBy(product => product.Price)
                     .ThenBy(product => product.Id),

            "name" when request.SortDescending =>
                query.OrderByDescending(product => product.Name)
                     .ThenBy(product => product.Id),

            _ =>
                query.OrderBy(product => product.Name)
                     .ThenBy(product => product.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)request.PageSize);

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new ProductListItemResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CategoryId,
                product.Category.Name))
            .ToListAsync(cancellationToken);

        return new GetProductsResponse(
            products,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);
    }
}