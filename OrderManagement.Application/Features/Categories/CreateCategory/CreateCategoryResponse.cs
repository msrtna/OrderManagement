
namespace OrderManagement.Application.Features.Categories.CreateCategory;

public sealed record CreateCategoryResponse(
    int Id,
    string Name,
    string? Description,
    bool IsActive);
