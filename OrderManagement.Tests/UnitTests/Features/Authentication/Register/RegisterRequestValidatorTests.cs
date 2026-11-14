using OrderManagement.Application.Features.Authentication.Register;

namespace OrderManagement.Tests.UnitTests.Features.Authentication.Register;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFirstNameIsEmpty()
    {
        // Arrange
        var request = new RegisterRequest(
            "",
            "Moradi",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.FirstName) &&
            error.ErrorCode == "NotEmptyValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFirstNameExceedsMaximumLength()
    {
        // Arrange
        var firstName = new string('A', 101);

        var request = new RegisterRequest(
            firstName,
            "Moradi",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.FirstName) &&
            error.ErrorCode == "MaximumLengthValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLastNameIsEmpty()
    {
        // Arrange
        var request = new RegisterRequest(
            "Mohammad",
            "",
            "mohammad@example.com",
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.LastName) &&
            error.ErrorCode == "NotEmptyValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLastNameExceedsMaximumLength()
    {
        // Arrange
        var lastName = new string('A', 101);

        var request = new RegisterRequest(
            "Mohammad",
            lastName,
            "mohammad@example.com",
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.LastName) &&
            error.ErrorCode == "MaximumLengthValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "",
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.Email) &&
            error.ErrorCode == "NotEmptyValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        // Arrange
        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "invalid-email",
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.Email) &&
            error.ErrorCode == "EmailValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailExceedsMaximumLength()
    {
        // Arrange
        var email = $"{new string('a', 245)}@example.com";

        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            email,
            "Password123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.Email) &&
            error.ErrorCode == "MaximumLengthValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        // Arrange
        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.Password) &&
            error.ErrorCode == "NotEmptyValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsShorterThanMinimumLength()
    {
        // Arrange
        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Pass123");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.Password) &&
            error.ErrorCode == "MinimumLengthValidator");
    }
}
