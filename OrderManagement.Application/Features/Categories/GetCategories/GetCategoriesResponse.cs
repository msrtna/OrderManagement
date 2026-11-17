
namespace OrderManagement.Application.Features.Categories.GetCategories;

public sealed record GetCategoriesResponse(
    IReadOnlyCollection<CategoryItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CategoryItemResponse(
    int Id,
    string Name,
    string? Description);
