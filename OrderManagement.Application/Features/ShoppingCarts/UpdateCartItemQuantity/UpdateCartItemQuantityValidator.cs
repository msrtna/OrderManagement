using FluentValidation;

namespace OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;

public sealed class UpdateCartItemQuantityValidator
    : AbstractValidator<UpdateCartItemQuantityRequest>
{
    public UpdateCartItemQuantityValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}