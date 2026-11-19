namespace OrderManagement.Application.Features.ShoppingCarts.ClearCart;

public sealed record ClearCartResponse(
    int RemovedItemsCount);