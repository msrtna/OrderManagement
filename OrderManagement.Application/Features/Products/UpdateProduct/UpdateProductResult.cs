namespace OrderManagement.Application.Features.Products.UpdateProduct;

public sealed record UpdateProductResult(
    bool Succeeded,
    IReadOnlyCollection<UpdateProductError> Errors);

public sealed record UpdateProductError(
    string ErrorCode,
    string ErrorMessage);