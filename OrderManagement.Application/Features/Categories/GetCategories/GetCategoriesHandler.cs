
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Categories.GetCategories;

public sealed class GetCategoriesHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<GetCategoriesRequest> _validator;

    public GetCategoriesHandler(
        IApplicationDbContext context,
        IValidator<GetCategoriesRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<GetCategoriesResult> HandleAsync(
        GetCategoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new GetCategoriesError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new GetCategoriesResult(
                false,
                null,
                errors);
        }

        var query = _context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();

            query = query.Where(category =>
                category.Name.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = request.SortDescending
            ? query.OrderByDescending(category => category.Name)
            : query.OrderBy(category => category.Name);

        var items = await orderedQuery
            .ThenBy(category => category.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(category => new CategoryItemResponse(
                category.Id,
                category.Name,
                category.Description))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)request.PageSize);

        var response = new GetCategoriesResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);

        return new GetCategoriesResult(
            true,
            response,
            Array.Empty<GetCategoriesError>());
    }
}
