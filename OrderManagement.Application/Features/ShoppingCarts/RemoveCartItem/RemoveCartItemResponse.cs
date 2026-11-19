namespace OrderManagement.Application.Features.ShoppingCarts.RemoveCartItem;

public sealed record RemoveCartItemResponse(
    long CartItemId,
    string ProductName);