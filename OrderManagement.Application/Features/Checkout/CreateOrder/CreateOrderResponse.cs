
namespace OrderManagement.Application.Features.Checkout.CreateOrder;

public sealed record CreateOrderResponse(
    long OrderId,
    string Status,
    int TotalItems,
    decimal ItemsTotal,
    decimal ShippingCost,
    decimal TotalAmount,
    DateTime ExpiresAt);
