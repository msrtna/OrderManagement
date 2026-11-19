
namespace OrderManagement.Application.Features.Checkout.CreateOrder;

public sealed record CreateOrderResult(
    bool Succeeded,
    CreateOrderResponse? Response,
    IReadOnlyCollection<CreateOrderError> Errors);
