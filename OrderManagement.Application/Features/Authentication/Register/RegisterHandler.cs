using FluentValidation;
using OrderManagement.Application.Abstractions.Identity;

namespace OrderManagement.Application.Features.Authentication.Register;

public sealed class RegisterHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<RegisterRequest> _validator;

    public RegisterHandler(
        IIdentityService identityService,
        IValidator<RegisterRequest> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<RegisterHandlerResult> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new RegisterError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToArray();

            return new RegisterHandlerResult(
                false,
                null,
                errors);
        }

        var identityResult = await _identityService.RegisterCustomerAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            cancellationToken);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors
                .Select(error => new RegisterError(
                    error.Code,
                    error.Description))
                .ToArray();

            return new RegisterHandlerResult(
                false,
                null,
                errors);
        }

        var response = new RegisterResponse(
            identityResult.UserId!,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Email.Trim());

        return new RegisterHandlerResult(
            true,
            response,
            Array.Empty<RegisterError>());
    }
}

public sealed record RegisterError(
    string Code,
    string Description);

public sealed record RegisterHandlerResult(
    bool Succeeded,
    RegisterResponse? Response,
    IReadOnlyList<RegisterError> Errors);