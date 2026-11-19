
namespace OrderManagement.Application.Features.ShoppingCarts.GetCart;

public sealed record GetCartResponse(
    long ShoppingCartId,
    IReadOnlyCollection<GetCartItemResponse> Items,
    int TotalItems,
    decimal TotalAmount);
