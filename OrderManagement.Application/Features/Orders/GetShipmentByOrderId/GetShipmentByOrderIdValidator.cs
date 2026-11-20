using FluentValidation;

namespace OrderManagement.Application.Features.Orders.GetShipmentByOrderId;

public sealed class GetShipmentByOrderIdValidator
    : AbstractValidator<GetShipmentByOrderIdRequest>
{
    public GetShipmentByOrderIdValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero.");
    }
}