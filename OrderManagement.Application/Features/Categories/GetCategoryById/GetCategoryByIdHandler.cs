
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Categories.GetCategoryById;

public sealed class GetCategoryByIdHandler
{
    private readonly IApplicationDbContext _context;

    public GetCategoryByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetCategoryByIdResponse?> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(category =>
                category.Id == id &&
                category.IsActive)
            .Select(category => new GetCategoryByIdResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
