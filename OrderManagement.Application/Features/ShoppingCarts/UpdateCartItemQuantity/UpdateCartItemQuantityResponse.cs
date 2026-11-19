namespace OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;

public sealed record UpdateCartItemQuantityResponse(
    long CartItemId,
    int ProductId,
    string ProductName,
    int Quantity);