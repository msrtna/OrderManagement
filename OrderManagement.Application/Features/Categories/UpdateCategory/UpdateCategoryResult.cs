
namespace OrderManagement.Application.Features.Categories.UpdateCategory;

public sealed record UpdateCategoryResult(
    bool Succeeded,
    IReadOnlyCollection<UpdateCategoryError> Errors);

public sealed record UpdateCategoryError(
    string ErrorCode,
    string ErrorMessage);
