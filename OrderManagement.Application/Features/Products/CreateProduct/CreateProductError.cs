namespace OrderManagement.Application.Features.Products.CreateProduct;

public sealed record CreateProductError(
    string ErrorCode,
    string ErrorMessage);