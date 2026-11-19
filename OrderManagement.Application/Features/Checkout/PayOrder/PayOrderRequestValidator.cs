using FluentValidation;

namespace OrderManagement.Application.Features.Checkout.PayOrder;

public sealed class PayOrderRequestValidator
    : AbstractValidator<PayOrderRequest>
{
    public PayOrderRequestValidator()
    {
        RuleFor(request => request.OrderId)
            .GreaterThan(0)
            .WithErrorCode("InvalidOrderId")
            .WithMessage("Order ID must be greater than zero.");
    }
}