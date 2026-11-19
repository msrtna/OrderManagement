
namespace OrderManagement.Application.Features.ShoppingCarts.AddToCart;

public sealed record AddToCartResponse(
    long CartItemId,
    int ProductId,
    string ProductName,
    int Quantity);
