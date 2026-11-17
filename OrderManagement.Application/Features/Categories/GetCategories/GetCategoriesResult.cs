
namespace OrderManagement.Application.Features.Categories.GetCategories;

public sealed record GetCategoriesResult(
    bool Succeeded,
    GetCategoriesResponse? Response,
    IReadOnlyCollection<GetCategoriesError> Errors);

public sealed record GetCategoriesError(
    string ErrorCode,
    string ErrorMessage);
