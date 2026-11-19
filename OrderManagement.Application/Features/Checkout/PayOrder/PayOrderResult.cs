namespace OrderManagement.Application.Features.Checkout.PayOrder;

public sealed record PayOrderResult(
    bool Succeeded,
    PayOrderResponse? Response,
    IReadOnlyCollection<PayOrderError> Errors);