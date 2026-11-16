namespace OrderManagement.Application.Features.Products.GetProductById;

public sealed record GetProductByIdResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string CategoryName);