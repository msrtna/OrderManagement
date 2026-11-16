namespace OrderManagement.Application.Features.Products.UpdateProduct;

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId);