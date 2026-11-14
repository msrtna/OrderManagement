namespace OrderManagement.Application.Abstractions.Identity;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(
        string userId,
        string email,
        IReadOnlyCollection<string> roles);

    RefreshTokenResult GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public sealed record AccessTokenResult(
    string Token,
    DateTime ExpiresAt);

public sealed record RefreshTokenResult(
    string Token,
    DateTime ExpiresAt);