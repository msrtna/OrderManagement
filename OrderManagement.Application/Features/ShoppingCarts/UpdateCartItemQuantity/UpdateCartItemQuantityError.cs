namespace OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;

public sealed record UpdateCartItemQuantityError(
    string ErrorCode,
    string ErrorMessage);