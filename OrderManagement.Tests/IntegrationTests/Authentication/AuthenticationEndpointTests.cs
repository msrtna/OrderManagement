
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions.Identity;

namespace OrderManagement.Tests.IntegrationTests.Authentication;

public sealed class AuthenticationEndpointTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationEndpointTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        // Arrange
        using var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUserInformation_WhenTokenIsValid()
    {
        // Arrange
        const string userId = "test-user-id";
        const string email = "test@example.com";
        const string role = "Customer";

        using var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var tokenService = scope.ServiceProvider
            .GetRequiredService<ITokenService>();

        var token = tokenService.GenerateAccessToken(
            userId,
            email,
            new[] { role });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            userId,
            result.GetProperty("userId").GetString());

        Assert.Equal(
            email,
            result.GetProperty("email").GetString());

        var roles = result.GetProperty("roles")
            .EnumerateArray()
            .Select(roleElement => roleElement.GetString())
            .ToArray();

        Assert.Contains(role, roles);
    }
}
