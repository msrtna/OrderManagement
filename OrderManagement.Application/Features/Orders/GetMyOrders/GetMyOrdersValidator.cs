using FluentValidation;

namespace OrderManagement.Application.Features.Orders.GetMyOrders;

public sealed class GetMyOrdersValidator : AbstractValidator<GetMyOrdersRequest>
{
    public GetMyOrdersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}