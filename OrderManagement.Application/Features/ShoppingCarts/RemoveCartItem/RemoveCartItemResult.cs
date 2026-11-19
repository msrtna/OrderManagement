namespace OrderManagement.Application.Features.ShoppingCarts.RemoveCartItem;

public sealed record RemoveCartItemResult(
    bool Succeeded,
    RemoveCartItemResponse? Response,
    IReadOnlyCollection<RemoveCartItemError> Errors);