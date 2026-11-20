using FluentValidation;

namespace OrderManagement.Application.Features.Orders.GetOrderStatusHistory;

public sealed class GetOrderStatusHistoryValidator
    : AbstractValidator<GetOrderStatusHistoryRequest>
{
    public GetOrderStatusHistoryValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero.");
    }
}