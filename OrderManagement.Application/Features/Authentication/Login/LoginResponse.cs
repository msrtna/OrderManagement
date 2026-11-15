namespace OrderManagement.Application.Features.Authentication.Login;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    string UserId,
    string FirstName,
    string LastName,
    string Email);