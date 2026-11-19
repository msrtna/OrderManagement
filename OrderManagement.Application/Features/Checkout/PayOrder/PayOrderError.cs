namespace OrderManagement.Application.Features.Checkout.PayOrder;

public sealed record PayOrderError(
    string Code,
    string Description);