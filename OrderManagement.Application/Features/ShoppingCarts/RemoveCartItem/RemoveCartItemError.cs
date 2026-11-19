namespace OrderManagement.Application.Features.ShoppingCarts.RemoveCartItem;

public sealed record RemoveCartItemError(
    string ErrorCode,
    string ErrorMessage);