
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Features.Authentication.Register;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Tests.IntegrationTests.Authentication;

public sealed class RegisterTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task Register_ShouldCreateCustomer_WhenRequestIsValid()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";

        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            email,
            Password);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.Equal("Mohammad", user.FirstName);
        Assert.Equal("Moradi", user.LastName);

        Assert.True(
            await userManager.IsInRoleAsync(user, "Customer"));

        Assert.False(
            await userManager.IsInRoleAsync(user, "Admin"));
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";

        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            email,
            Password);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        // Act
        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordIsInvalid()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";

        var request = new RegisterRequest(
            "Mohammad",
            "Moradi",
            email,
            "123");

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        Assert.Null(user);
    }

    [Theory]
    [InlineData("", "Moradi", "test@example.com", "Password123!")]
    [InlineData("Mohammad", "", "test@example.com", "Password123!")]
    [InlineData("Mohammad", "Moradi", "", "Password123!")]
    [InlineData("Mohammad", "Moradi", "invalid-email", "Password123!")]
    [InlineData("Mohammad", "Moradi", "test@example.com", "")]
    public async Task Register_ShouldReturnBadRequest_WhenRequestIsInvalid(
    string firstName,
    string lastName,
    string email,
    string password)
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new RegisterRequest(
            firstName,
            lastName,
            email,
            password);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
