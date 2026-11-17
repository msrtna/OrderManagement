namespace OrderManagement.Application.Features.Products.CreateProduct;

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId);