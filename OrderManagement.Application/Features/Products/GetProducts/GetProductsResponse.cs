namespace OrderManagement.Application.Features.Products.GetProducts;

public sealed record GetProductsResponse(
    IReadOnlyCollection<ProductListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ProductListItemResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string CategoryName);