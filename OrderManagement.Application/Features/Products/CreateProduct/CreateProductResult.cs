namespace OrderManagement.Application.Features.Products.CreateProduct;

public sealed record CreateProductResult(
    bool Succeeded,
    CreateProductResponse? Response,
    IReadOnlyCollection<CreateProductError> Errors);