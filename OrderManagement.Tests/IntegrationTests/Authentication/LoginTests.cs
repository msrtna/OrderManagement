
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Features.Authentication.Login;
using OrderManagement.Application.Features.Authentication.Register;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Tests.IntegrationTests.Authentication;

public sealed class LoginTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Mohammad",
                "Moradi",
                email,
                Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, Password));

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

        Assert.NotEqual(default, result.AccessTokenExpiresAt);
        Assert.NotEqual(default, result.RefreshTokenExpiresAt);

        Assert.False(string.IsNullOrWhiteSpace(result.UserId));
        Assert.Equal("Mohammad", result.FirstName);
        Assert.Equal("Moradi", result.LastName);
        Assert.Equal(email, result.Email);

        Assert.True(result.AccessTokenExpiresAt > DateTime.UtcNow);
        Assert.True(result.RefreshTokenExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsIncorrect()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Mohammad",
                "Moradi",
                email,
                Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "WrongPassword123!"));

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenEmailDoesNotExist()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"unknown-{Guid.NewGuid():N}@example.com";

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, Password));

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserIsBlocked()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"blocked-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Blocked",
                "User",
                email,
                Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByEmailAsync(email);

            Assert.NotNull(user);

            user.IsBlocked = true;

            var updateResult = await userManager.UpdateAsync(user);

            Assert.True(
                updateResult.Succeeded,
                string.Join(
                    "; ",
                    updateResult.Errors.Select(error => error.Description)));
        }

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, Password));

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}
