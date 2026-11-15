using Moq;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Authentication.Login;

namespace OrderManagement.Tests.UnitTests.Features.Authentication.Login;

public class LoginHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();

    private const string UserId = "user-123";
    private const string Email = "mohammad@example.com";
    private const string RawRefreshToken = "refresh-token";
    private const string HashedRefreshToken = "hashed-refresh-token";

    private readonly DateTime _accessTokenExpiresAt =
        new(2026, 11, 15, 3, 0, 0, DateTimeKind.Utc);

    private readonly DateTime _refreshTokenExpiresAt =
        new(2026, 11, 22, 3, 0, 0, DateTimeKind.Utc);

    private LoginHandler CreateHandler()
    {
        var validator = new LoginRequestValidator();

        return new LoginHandler(
            validator,
            _identityServiceMock.Object,
            _tokenServiceMock.Object);
    }

    private void SetupSuccessfulAuthentication(
        string userId = UserId,
        string email = Email,
        string firstName = "Mohammad",
        string lastName = "Moradi",
        IReadOnlyCollection<string>? roles = null)
    {
        _identityServiceMock
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginIdentityResult(
                true,
                userId,
                firstName,
                lastName,
                email,
                (IReadOnlyList<string>)(roles ?? new[] { "Customer" }),
                Array.Empty<IdentityError>()));
    }

    private void SetupTokenGeneration()
    {
        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(new AccessTokenResult(
                "access-token",
                _accessTokenExpiresAt));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(new RefreshTokenResult(
                RawRefreshToken,
                _refreshTokenExpiresAt));

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken(RawRefreshToken))
            .Returns(HashedRefreshToken);

        _identityServiceMock
            .Setup(x => x.StoreRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        SetupSuccessfulAuthentication();
        SetupTokenGeneration();

        var handler = CreateHandler();

        var request = new LoginRequest(
            Email,
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("access-token", result.Response.AccessToken);
        Assert.Equal(RawRefreshToken, result.Response.RefreshToken);

        Assert.Equal(_accessTokenExpiresAt, result.Response.AccessTokenExpiresAt);
        Assert.Equal(_refreshTokenExpiresAt, result.Response.RefreshTokenExpiresAt);

        Assert.Equal(UserId, result.Response.UserId);
        Assert.Equal("Mohammad", result.Response.FirstName);
        Assert.Equal("Moradi", result.Response.LastName);
        Assert.Equal(Email, result.Response.Email);

        Assert.Empty(result.Errors);

        _identityServiceMock.Verify(
            x => x.AuthenticateAsync(
                Email,
                "Password123",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(
                UserId,
                Email,
                It.IsAny<IReadOnlyCollection<string>>()),
            Times.Once);

        _tokenServiceMock.Verify(
            x => x.GenerateRefreshToken(),
            Times.Once);

        _identityServiceMock.Verify(
            x => x.StoreRefreshTokenAsync(
                UserId,
                HashedRefreshToken,
                _refreshTokenExpiresAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreHashedRefreshToken_WhenLoginIsSuccessful()
    {
        // Arrange
        SetupSuccessfulAuthentication();
        SetupTokenGeneration();

        var handler = CreateHandler();

        var request = new LoginRequest(
            Email,
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);

        _tokenServiceMock.Verify(
            x => x.HashRefreshToken(RawRefreshToken),
            Times.Once);

        _identityServiceMock.Verify(
            x => x.StoreRefreshTokenAsync(
                UserId,
                It.Is<string>(hash =>
                    hash == HashedRefreshToken &&
                    hash != RawRefreshToken),
                _refreshTokenExpiresAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnErrors_WhenValidationFails()
    {
        // Arrange
        var handler = CreateHandler();

        var request = new LoginRequest(
            "invalid-email",
            "");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.NotEmpty(result.Errors);

        Assert.Contains(result.Errors, error =>
            error.Code == "EmailValidator");

        Assert.Contains(result.Errors, error =>
            error.Code == "NotEmptyValidator");

        Assert.All(result.Errors, error =>
            Assert.False(string.IsNullOrWhiteSpace(error.Description)));

        _identityServiceMock.Verify(
            x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _identityServiceMock.Verify(
            x => x.StoreRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCallIdentity_WhenValidationFails()
    {
        // Arrange
        var handler = CreateHandler();

        var request = new LoginRequest(
            "",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);

        _identityServiceMock.Verify(
            x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenAuthenticationFails()
    {
        // Arrange
        _identityServiceMock
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginIdentityResult(
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                    new IdentityError(
                        "InvalidCredentials",
                        "Invalid email or password.")
                }));

        var handler = CreateHandler();

        var request = new LoginRequest(
            Email,
            "WrongPassword123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(result.Errors, error =>
            error.Code == "InvalidCredentials");

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);

        _tokenServiceMock.Verify(
            x => x.GenerateRefreshToken(),
            Times.Never);

        _identityServiceMock.Verify(
            x => x.StoreRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapIdentityErrors_WhenAuthenticationFails()
    {
        // Arrange
        var identityErrors = new[]
        {
            new IdentityError(
                "InvalidCredentials",
                "Invalid email or password."),

            new IdentityError(
                "AccountLocked",
                "Account is temporarily locked.")
        };

        _identityServiceMock
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginIdentityResult(
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                identityErrors));

        var handler = CreateHandler();

        var request = new LoginRequest(
            Email,
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Equal(2, result.Errors.Count);

        Assert.Contains(result.Errors, error =>
            error.Code == "InvalidCredentials" &&
            error.Description == "Invalid email or password.");

        Assert.Contains(result.Errors, error =>
            error.Code == "AccountLocked" &&
            error.Description == "Account is temporarily locked.");

        _identityServiceMock.Verify(
            x => x.StoreRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassUserRolesToTokenService_WhenAuthenticationSucceeds()
    {
        // Arrange
        var roles = new[] { "Customer", "Admin" };

        SetupSuccessfulAuthentication(roles: roles);
        SetupTokenGeneration();

        var handler = CreateHandler();

        var request = new LoginRequest(
            Email,
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(
                UserId,
                Email,
                It.Is<IReadOnlyCollection<string>>(
                    actualRoles => actualRoles.SequenceEqual(roles))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseIdentityUserInformation_WhenAuthenticationSucceeds()
    {
        // Arrange
        SetupSuccessfulAuthentication(
            userId: "user-456",
            email: "ali@example.com",
            firstName: "Ali",
            lastName: "Ahmadi");

        SetupTokenGeneration();

        var handler = CreateHandler();

        var request = new LoginRequest(
            "ali@example.com",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("user-456", result.Response.UserId);
        Assert.Equal("Ali", result.Response.FirstName);
        Assert.Equal("Ahmadi", result.Response.LastName);
        Assert.Equal("ali@example.com", result.Response.Email);

        _identityServiceMock.Verify(
            x => x.StoreRefreshTokenAsync(
                "user-456",
                HashedRefreshToken,
                _refreshTokenExpiresAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
