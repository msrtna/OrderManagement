
namespace OrderManagement.Application.Features.ShoppingCarts.AddToCart;

public sealed record AddToCartError(
    string ErrorCode,
    string ErrorMessage);
