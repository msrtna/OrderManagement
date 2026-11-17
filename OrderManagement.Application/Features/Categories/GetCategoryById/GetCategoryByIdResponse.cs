
namespace OrderManagement.Application.Features.Categories.GetCategoryById;

public sealed record GetCategoryByIdResponse(
    int Id,
    string Name,
    string? Description,
    bool IsActive);
