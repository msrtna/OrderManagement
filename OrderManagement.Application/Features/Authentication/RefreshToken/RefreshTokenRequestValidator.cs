using FluentValidation;

namespace OrderManagement.Application.Features.Authentication.RefreshToken;

public sealed class RefreshTokenRequestValidator
    : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithErrorCode("RefreshTokenRequired")
            .MaximumLength(512)
            .WithErrorCode("RefreshTokenTooLong");
    }
}