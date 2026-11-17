
namespace OrderManagement.Application.Features.Categories.CreateCategory;

public sealed record CreateCategoryResult(
    bool Succeeded,
    CreateCategoryResponse? Response,
    IReadOnlyCollection<CreateCategoryError> Errors);

public sealed record CreateCategoryError(
    string ErrorCode,
    string ErrorMessage);
