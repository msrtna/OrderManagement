using Moq;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Authentication.Login;

namespace OrderManagement.Tests.UnitTests.Features.Authentication.Login;

public class LoginHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();

    private LoginHandler CreateHandler()
    {
        var validator = new LoginRequestValidator();

        return new LoginHandler(
            validator,
            _identityServiceMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        _identityServiceMock
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginIdentityResult(
                true,
                "user-123",
                "Mohammad",
                "Moradi",
                "mohammad@example.com",
                new[] { "Customer" },
                Array.Empty<IdentityError>()));

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                "user-123",
                "mohammad@example.com",
                It.Is<IReadOnlyCollection<string>>(
                    roles => roles.Contains("Customer"))))
            .Returns(new AccessTokenResult(
                "access-token",
                DateTime.UtcNow.AddMinutes(15)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(new RefreshTokenResult(
                "refresh-token",
                DateTime.UtcNow.AddDays(7)));

        var handler = CreateHandler();

        var request = new LoginRequest(
            "mohammad@example.com",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("access-token", result.Response.AccessToken);
        Assert.Equal("refresh-token", result.Response.RefreshToken);

        Assert.Equal("user-123", result.Response.UserId);
        Assert.Equal("Mohammad", result.Response.FirstName);
        Assert.Equal("Moradi", result.Response.LastName);
        Assert.Equal("mohammad@example.com", result.Response.Email);

        Assert.Empty(result.Errors);

        _identityServiceMock.Verify(
            x => x.AuthenticateAsync(
                "mohammad@example.com",
                "Password123",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(
                "user-123",
                "mohammad@example.com",
                It.IsAny<IReadOnlyCollection<string>>()),
            Times.Once);

        _tokenServiceMock.Verify(
            x => x.GenerateRefreshToken(),
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
            "mohammad@example.com",
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
            "mohammad@example.com",
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
    }

    [Fact]
    public async Task HandleAsync_ShouldPassUserRolesToTokenService_WhenAuthenticationSucceeds()
    {
        // Arrange
        var roles = new[] { "Customer", "Admin" };

        _identityServiceMock
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginIdentityResult(
                true,
                "user-123",
                "Mohammad",
                "Moradi",
                "mohammad@example.com",
                roles,
                Array.Empty<IdentityError>()));

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(new AccessTokenResult(
                "access-token",
                DateTime.UtcNow.AddMinutes(15)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(new RefreshTokenResult(
                "refresh-token",
                DateTime.UtcNow.AddDays(7)));

        var handler = CreateHandler();

        var request = new LoginRequest(
            "mohammad@example.com",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(
                "user-123",
                "mohammad@example.com",
                It.Is<IReadOnlyCollection<string>>(
                    actualRoles =>
                        actualRoles.SequenceEqual(roles))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseIdentityUserInformation_WhenAuthenticationSucceeds()
    {
        // Arrange
        _identityServiceMock
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginIdentityResult(
                true,
                "user-456",
                "Ali",
                "Ahmadi",
                "ali@example.com",
                new[] { "Customer" },
                Array.Empty<IdentityError>()));

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(new AccessTokenResult(
                "access-token",
                DateTime.UtcNow.AddMinutes(15)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(new RefreshTokenResult(
                "refresh-token",
                DateTime.UtcNow.AddDays(7)));

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
    }
}
