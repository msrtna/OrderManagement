using FluentValidation;

namespace OrderManagement.Application.Features.Authentication.Login
{
    public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
