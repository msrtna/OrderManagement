
using FluentValidation;

namespace OrderManagement.Application.Features.Checkout.CreateOrder;

public sealed class CreateOrderRequestValidator
    : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.AddressId)
            .GreaterThan(0)
            .WithErrorCode("InvalidAddressId")
            .WithMessage("Address ID must be greater than zero.");
    }
}
