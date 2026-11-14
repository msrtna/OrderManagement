
using FluentValidation.TestHelper;
using OrderManagement.Application.Features.Authentication.Login;
using Xunit;

namespace OrderManagement.Tests.UnitTests.Features.Authentication.Login;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        // Arrange
        var request = new LoginRequest(
            "customer@example.com",
            "StrongPassword123!");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var request = new LoginRequest(
            "",
            "StrongPassword123!");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailFormatIsInvalid()
    {
        // Arrange
        var request = new LoginRequest(
            "invalid-email",
            "StrongPassword123!");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("EmailValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailExceedsMaximumLength()
    {
        // Arrange
        var email = new string('a', 250) + "@test.com";

        var request = new LoginRequest(
            email,
            "StrongPassword123!");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        // Arrange
        var request = new LoginRequest(
            "customer@example.com",
            "");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("NotEmptyValidator");
    }
}
