using FluentValidation;
using OrderManagement.Application.Abstractions.Identity;

namespace OrderManagement.Application.Features.Authentication.Login;

public sealed class LoginHandler
{
    private readonly IValidator<LoginRequest> _validator;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public LoginHandler(
        IValidator<LoginRequest> validator,
        IIdentityService identityService,
        ITokenService tokenService)
    {
        _validator = validator;
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<LoginHandlerResult> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var validationErrors = validationResult.Errors
                .Select(error => new LoginError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .ToArray();

            return new LoginHandlerResult(
                false,
                null,
                validationErrors);
        }

        var identityResult = await _identityService.AuthenticateAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!identityResult.Succeeded)
        {
            var identityErrors = identityResult.Errors
                .Select(error => new LoginError(
                    error.Code,
                    error.Description))
                .ToArray();

            return new LoginHandlerResult(
                false,
                null,
                identityErrors);
        }

        var accessToken = _tokenService.GenerateAccessToken(
            identityResult.UserId!,
            identityResult.Email!,
            identityResult.Roles);

        var refreshToken = _tokenService.GenerateRefreshToken();

        var response = new LoginResponse(
            accessToken.Token,
            refreshToken.Token,
            accessToken.ExpiresAt,
            refreshToken.ExpiresAt,
            identityResult.UserId!,
            identityResult.FirstName!,
            identityResult.LastName!,
            identityResult.Email!);

        return new LoginHandlerResult(
            true,
            response,
            Array.Empty<LoginError>());
    }
}

public sealed record LoginError(
    string Code,
    string Description);

public sealed record LoginHandlerResult(
    bool Succeeded,
    LoginResponse? Response,
    IReadOnlyList<LoginError> Errors);