
namespace OrderManagement.Application.Features.ShoppingCarts.AddToCart;

public sealed record AddToCartResult(
    bool Succeeded,
    AddToCartResponse? Response,
    IReadOnlyCollection<AddToCartError> Errors);
