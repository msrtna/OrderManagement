using FluentValidation;

namespace OrderManagement.Application.Features.Orders.GetOrderById;

public sealed class GetOrderByIdValidator : AbstractValidator<GetOrderByIdRequest>
{
    public GetOrderByIdValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero.");
    }
}