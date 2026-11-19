
using FluentValidation;

namespace OrderManagement.Application.Features.ShoppingCarts.AddToCart;

public sealed class AddToCartValidator
    : AbstractValidator<AddToCartRequest>
{
    public AddToCartValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .WithMessage("A valid product must be selected.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}
