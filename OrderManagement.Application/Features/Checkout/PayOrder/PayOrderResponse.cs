namespace OrderManagement.Application.Features.Checkout.PayOrder;

public sealed record PayOrderResponse(
    long OrderId,
    string OrderStatus,
    long PaymentAttemptId,
    string PaymentStatus,
    decimal Amount,
    DateTime PaidAt);