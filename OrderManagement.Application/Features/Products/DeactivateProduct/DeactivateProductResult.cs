namespace OrderManagement.Application.Features.Products.DeactivateProduct;

public sealed record DeactivateProductResult(
    bool Succeeded,
    IReadOnlyCollection<DeactivateProductError> Errors);

public sealed record DeactivateProductError(
    string ErrorCode,
    string ErrorMessage);