
namespace OrderManagement.Application.Features.ShoppingCarts.GetCart;

public sealed record GetCartItemResponse(
    long CartItemId,
    int? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    bool IsProductDeleted);
