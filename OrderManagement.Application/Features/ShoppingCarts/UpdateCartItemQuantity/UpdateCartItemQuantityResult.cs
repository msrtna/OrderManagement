namespace OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;

public sealed record UpdateCartItemQuantityResult(
    bool Succeeded,
    UpdateCartItemQuantityResponse? Response,
    IReadOnlyCollection<UpdateCartItemQuantityError> Errors);