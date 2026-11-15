
using FluentValidation;
using OrderManagement.Application.Abstractions.Identity;

namespace OrderManagement.Application.Features.Authentication.RefreshToken;

public sealed class RefreshTokenHandler
{
    private readonly IValidator<RefreshTokenRequest> _validator;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public RefreshTokenHandler(
        IValidator<RefreshTokenRequest> validator,
        IIdentityService identityService,
        ITokenService tokenService)
    {
        _validator = validator;
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<RefreshTokenHandlerResult> HandleAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new RefreshTokenError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .ToArray();

            return new RefreshTokenHandlerResult(
                false,
                null,
                errors);
        }

        var oldTokenHash = _tokenService.HashRefreshToken(
            request.RefreshToken);

        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var newTokenHash = _tokenService.HashRefreshToken(
            newRefreshToken.Token);

        var identityResult =
            await _identityService.RotateRefreshTokenAsync(
                oldTokenHash,
                newTokenHash,
                newRefreshToken.ExpiresAt,
                cancellationToken);

        if (!identityResult.Succeeded)
        {
            var identityErrors = identityResult.Errors
                .Select(error => new RefreshTokenError(
                    error.Code,
                    error.Description))
                .ToArray();

            return new RefreshTokenHandlerResult(
                false,
                null,
                identityErrors);
        }

        var accessToken = _tokenService.GenerateAccessToken(
            identityResult.UserId!,
            identityResult.Email!,
            identityResult.Roles);

        var response = new RefreshTokenResponse(
            accessToken.Token,
            newRefreshToken.Token,
            accessToken.ExpiresAt,
            newRefreshToken.ExpiresAt,
            identityResult.UserId!,
            identityResult.FirstName!,
            identityResult.LastName!,
            identityResult.Email!);

        return new RefreshTokenHandlerResult(
            true,
            response,
            Array.Empty<RefreshTokenError>());
    }
}

public sealed record RefreshTokenError(
    string Code,
    string Description);

public sealed record RefreshTokenHandlerResult(
    bool Succeeded,
    RefreshTokenResponse? Response,
    IReadOnlyList<RefreshTokenError> Errors);

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    string UserId,
    string FirstName,
    string LastName,
    string Email);
