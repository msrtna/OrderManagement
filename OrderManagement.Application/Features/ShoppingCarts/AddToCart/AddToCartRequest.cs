
namespace OrderManagement.Application.Features.ShoppingCarts.AddToCart;

public sealed record AddToCartRequest(
    int ProductId,
    int Quantity);
