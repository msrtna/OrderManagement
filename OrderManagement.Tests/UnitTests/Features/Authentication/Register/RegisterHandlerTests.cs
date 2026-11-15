using Moq;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Authentication.Register;

namespace OrderManagement.Tests.UnitTests.Features.Authentication.Register;

public class RegisterHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();

    private RegisterHandler CreateHandler()
    {
        var validator = new RegisterRequestValidator();

        return new RegisterHandler(
            _identityServiceMock.Object,
            validator);
    }

    [Fact]
    public async Task HandleAsync_ShouldRegisterUser_WhenRequestIsValid()
    {
        // Arrange
        _identityServiceMock
            .Setup(x => x.RegisterCustomerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterResult(
                true,
                "user-123",
                Array.Empty<IdentityError>()));

        var handler = CreateHandler();

        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("user-123", result.Response.UserId);
        Assert.Equal("Mohammad", result.Response.FirstName);
        Assert.Equal("Moradi", result.Response.LastName);
        Assert.Equal("mohammad@example.com", result.Response.Email);

        Assert.Empty(result.Errors);

        _identityServiceMock.Verify(
            x => x.RegisterCustomerAsync(
                "Mohammad",
                "Moradi",
                "mohammad@example.com",
                "Password123",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnErrors_WhenValidationFails()
    {
        // Arrange
        var handler = CreateHandler();

        var request = new RegisterRequest(
            "",
            "",
            "invalid-email",
            "123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.NotEmpty(result.Errors);

        Assert.Contains(result.Errors, error =>
            error.Code == "NotEmptyValidator");

        Assert.Contains(result.Errors, error =>
            error.Code == "EmailValidator");

        Assert.Contains(result.Errors, error =>
            error.Code == "MinimumLengthValidator");

        Assert.All(result.Errors, error =>
            Assert.False(string.IsNullOrWhiteSpace(error.Description)));
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCallIdentity_WhenValidationFails()
    {
        // Arrange
        var handler = CreateHandler();

        var request = new RegisterRequest(
            "",
            "Moradi",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);

        _identityServiceMock.Verify(
            x => x.RegisterCustomerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenIdentityRegistrationFails()
    {
        // Arrange
        _identityServiceMock
            .Setup(x => x.RegisterCustomerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterResult(
                false,
                null,
                new[]
                {
                    new IdentityError(
                        "DuplicateEmail",
                        "Email is already registered.")
                }));

        var handler = CreateHandler();

        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.NotEmpty(result.Errors);

        _identityServiceMock.Verify(
            x => x.RegisterCustomerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapIdentityErrors_WhenRegistrationFails()
    {
        // Arrange
        var identityErrors = new[]
        {
            new IdentityError(
                "DuplicateEmail",
                "Email is already registered."),

            new IdentityError(
                "PasswordTooShort",
                "Password does not meet requirements.")
        };

        _identityServiceMock
            .Setup(x => x.RegisterCustomerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterResult(
                false,
                null,
                identityErrors));

        var handler = CreateHandler();

        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Equal(2, result.Errors.Count);

        Assert.Contains(result.Errors, error =>
            error.Code == "DuplicateEmail" &&
            error.Description == "Email is already registered.");

        Assert.Contains(result.Errors, error =>
            error.Code == "PasswordTooShort" &&
            error.Description == "Password does not meet requirements.");
    }

    [Fact]
    public async Task HandleAsync_ShouldTrimResponseFields_WhenRegistrationSucceeds()
    {
        // Arrange
        _identityServiceMock
            .Setup(x => x.RegisterCustomerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterResult(
                true,
                "user-123",
                Array.Empty<IdentityError>()));

        var handler = CreateHandler();

        var request = new RegisterRequest(
            "  Mohammad  ",
            "  Moradi  ",
            "  mohammad@example.com  ",
            "Password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("Mohammad", result.Response.FirstName);
        Assert.Equal("Moradi", result.Response.LastName);
        Assert.Equal("mohammad@example.com", result.Response.Email);
    }
}
