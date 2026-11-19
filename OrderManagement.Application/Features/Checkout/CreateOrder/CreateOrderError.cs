
namespace OrderManagement.Application.Features.Checkout.CreateOrder;

public sealed record CreateOrderError(
    string Code,
    string Description);
