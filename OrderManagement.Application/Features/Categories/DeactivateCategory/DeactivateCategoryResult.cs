
namespace OrderManagement.Application.Features.Categories.DeactivateCategory;

public sealed record DeactivateCategoryResult(
    bool Succeeded,
    IReadOnlyCollection<DeactivateCategoryError> Errors);

public sealed record DeactivateCategoryError(
    string ErrorCode,
    string ErrorMessage);
