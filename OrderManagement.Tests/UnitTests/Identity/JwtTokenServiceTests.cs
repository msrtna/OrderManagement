
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Infrastructure.Identity;
using Xunit;

namespace OrderManagement.Tests.UnitTests.Identity;

public sealed class JwtTokenServiceTests
{
    private readonly JwtOptions _jwtOptions = new()
    {
        Issuer = "OrderManagement",
        Audience = "OrderManagementClient",
        SecretKey = "ThisIsASecretKeyWithMoreThan32Characters!",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private JwtTokenService CreateService()
    {
        return new JwtTokenService(
            Options.Create(_jwtOptions));
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GenerateAccessToken(
            "user-123",
            "customer@example.com",
            new[] { "Customer" });

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);

        Assert.Equal(_jwtOptions.Issuer, token.Issuer);
        Assert.Contains(_jwtOptions.Audience, token.Audiences);
        Assert.Equal("user-123", token.Subject);
        Assert.Equal("customer@example.com",
            token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserRolesInClaims()
    {
        // Arrange
        var service = CreateService();
        var roles = new[] { "Customer", "Admin" };

        // Act
        var result = service.GenerateAccessToken(
            "user-123",
            "customer@example.com",
            roles);

        // Assert
        var token = new JwtSecurityTokenHandler()
            .ReadJwtToken(result.Token);

        foreach (var role in roles)
        {
            Assert.Contains(
                token.Claims,
                claim => claim.Type == ClaimTypes.Role
                         && claim.Value == role);
        }
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetCorrectExpirationTime()
    {
        // Arrange
        var service = CreateService();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var result = service.GenerateAccessToken(
            "user-123",
            "customer@example.com",
            Array.Empty<string>());

        var afterGeneration = DateTime.UtcNow;

        // Assert
        Assert.InRange(
            result.ExpiresAt,
            beforeGeneration.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            afterGeneration.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnRandomBase64Token()
    {
        // Arrange
        var service = CreateService();

        // Act
        var firstToken = service.GenerateRefreshToken();
        var secondToken = service.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(firstToken.Token));
        Assert.NotEqual(firstToken.Token, secondToken.Token);

        var decodedBytes = Convert.FromBase64String(firstToken.Token);

        Assert.Equal(64, decodedBytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldSetCorrectExpirationTime()
    {
        // Arrange
        var service = CreateService();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var result = service.GenerateRefreshToken();

        var afterGeneration = DateTime.UtcNow;

        // Assert
        Assert.InRange(
            result.ExpiresAt,
            beforeGeneration.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            afterGeneration.AddDays(_jwtOptions.RefreshTokenExpirationDays));
    }
}
