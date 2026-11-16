namespace OrderManagement.Application.Features.Products.CreateProduct;

public sealed record CreateProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    bool IsActive);