
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Features.Authentication.Login;
using OrderManagement.Application.Features.Authentication.Register;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Tests.IntegrationTests.Authentication;

public sealed class AuthorizationTests
{
    [Fact]
    public async Task AdminDashboard_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task AdminDashboard_ShouldReturnForbidden_WhenUserIsCustomer()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"customer-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Test",
                "Customer",
                email,
                password));

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);

        // Act
        var response = await client.GetAsync("/api/admin/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AdminDashboard_ShouldReturnOk_WhenUserIsAdmin()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string email = "admin-test@example.com";
        const string password = "AdminPassword123!";

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Admin"
            };

            var createResult = await userManager.CreateAsync(
                admin,
                password);

            Assert.True(
                createResult.Succeeded,
                string.Join(
                    "; ",
                    createResult.Errors.Select(error => error.Description)));

            var roleResult = await userManager.AddToRoleAsync(
                admin,
                "Admin");

            Assert.True(
                roleResult.Succeeded,
                string.Join(
                    "; ",
                    roleResult.Errors.Select(error => error.Description)));
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);

        // Act
        var response = await client.GetAsync("/api/admin/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }
}
