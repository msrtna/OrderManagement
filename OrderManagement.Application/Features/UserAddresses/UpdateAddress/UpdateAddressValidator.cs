
using FluentValidation;

namespace OrderManagement.Application.Features.UserAddresses.UpdateAddress;

public sealed class UpdateAddressValidator
    : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RecipientName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Province)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.AddressLine)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .MaximumLength(20);
    }
}
