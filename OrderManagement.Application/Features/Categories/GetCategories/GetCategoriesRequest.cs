
namespace OrderManagement.Application.Features.Categories.GetCategories;

public sealed record GetCategoriesRequest(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    bool SortDescending = false);
