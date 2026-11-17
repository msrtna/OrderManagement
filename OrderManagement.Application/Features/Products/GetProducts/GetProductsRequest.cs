namespace OrderManagement.Application.Features.Products.GetProducts;

public sealed record GetProductsRequest(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    int? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string SortBy = "Name",
    bool SortDescending = false);