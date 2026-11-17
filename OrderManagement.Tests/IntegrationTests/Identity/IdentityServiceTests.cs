using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Tests.IntegrationTests.Identity;

public class IdentityServiceTests
{
    [Fact]
    public async Task RegisterCustomerAsync_ShouldCreateUser_WhenRegistrationIsValid()
    {
        // Arrange
        await using var fixture =
            await IdentityServiceTestFixture.CreateAsync();

        await fixture.SeedCustomerRoleAsync();

        // Act
        var result = await fixture.IdentityService.RegisterCustomerAsync(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Password123!");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.UserId);
        Assert.Empty(result.Errors);

        var user = await fixture.UserManager.FindByIdAsync(
            result.UserId);

        Assert.NotNull(user);
        Assert.Equal("Mohammad", user.FirstName);
        Assert.Equal("Moradi", user.LastName);
        Assert.Equal("mohammad@example.com", user.Email);
        Assert.Equal("mohammad@example.com", user.UserName);

        Assert.False(user.IsBlocked);
        Assert.True(user.CanPurchase);
        Assert.True(user.LockoutEnabled);
    }

    [Fact]
    public async Task RegisterCustomerAsync_ShouldAssignCustomerRole_WhenRegistrationIsSuccessful()
    {
        // Arrange
        await using var fixture =
            await IdentityServiceTestFixture.CreateAsync();

        await fixture.SeedCustomerRoleAsync();

        // Act
        var result = await fixture.IdentityService.RegisterCustomerAsync(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Password123!");

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.UserId);

        var user = await fixture.UserManager.FindByIdAsync(
            result.UserId);

        Assert.NotNull(user);

        var roles = await fixture.UserManager.GetRolesAsync(user);

        Assert.Contains("Customer", roles);
    }

    [Fact]
    public async Task RegisterCustomerAsync_ShouldFail_WhenCustomerRoleDoesNotExist()
    {
        // Arrange
        await using var fixture =
            await IdentityServiceTestFixture.CreateAsync();

        // Customer role is intentionally not created.

        // Act
        var result = await fixture.IdentityService.RegisterCustomerAsync(
            "Mohammad",
            "Moradi",
            "mohammad@example.com",
            "Password123!");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.UserId);

        Assert.Contains(result.Errors, error =>
            error.Code == "CustomerRoleNotConfigured");

        var user = await fixture.UserManager.FindByEmailAsync(
            "mohammad@example.com");

        Assert.Null(user);
    }

    [Fact]
    public async Task RegisterCustomerAsync_ShouldFail_WhenEmailAlreadyExists()
    {
        // Arrange
        await using var fixture =
            await IdentityServiceTestFixture.CreateAsync();

        await fixture.SeedCustomerRoleAsync();

        var firstResult =
            await fixture.IdentityService.RegisterCustomerAsync(
                "Mohammad",
                "Moradi",
                "mohammad@example.com",
                "Password123!");

        Assert.True(firstResult.Succeeded);

        // Act
        var secondResult =
            await fixture.IdentityService.RegisterCustomerAsync(
                "Another",
                "User",
                "mohammad@example.com",
                "Password123!");

        // Assert
        Assert.False(secondResult.Succeeded);
        Assert.Null(secondResult.UserId);

        Assert.Contains(secondResult.Errors, error =>
            error.Code == "DuplicateEmail");

        var users = await fixture.DbContext.Users
            .Where(user => user.Email == "mohammad@example.com")
            .ToListAsync();

        Assert.Single(users);
    }

    [Fact]
    public async Task RegisterCustomerAsync_ShouldRollbackUserCreation_WhenRoleAssignmentThrows()
    {
        // Arrange
        await using var fixture =
            await IdentityServiceTestFixture.CreateAsync();

        await fixture.SeedCustomerRoleAsync();

        // Simulate a database failure while inserting the user role.
        await fixture.DbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER FailUserRoleInsert
            BEFORE INSERT ON AspNetUserRoles
            BEGIN
                SELECT RAISE(ABORT, 'Simulated role assignment failure');
            END;
            """);

        // Act
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await fixture.IdentityService.RegisterCustomerAsync(
                "Mohammad",
                "Moradi",
                "mohammad@example.com",
                "Password123!"));

        // Assert
        // The transaction should be rolled back when disposed.
        var user = await fixture.UserManager.FindByEmailAsync(
            "mohammad@example.com");

        Assert.Null(user);

        var userCount = await fixture.DbContext.Users.CountAsync();

        Assert.Equal(0, userCount);
    }


    [Fact]
    public async Task StoreRefreshTokenAsync_ShouldPersistRefreshToken_WhenUserExists()
    {
        // Arrange
        await using var fixture =
            await IdentityServiceTestFixture.CreateAsync();

        var user = new ApplicationUser
        {
            FirstName = "Mohammad",
            LastName = "Moradi",
            Email = "mohammad@example.com",
            UserName = "mohammad@example.com"
        };

        var createResult = await fixture.UserManager.CreateAsync(
            user,
            "Password123!");

        Assert.True(
            createResult.Succeeded,
            string.Join(", ", createResult.Errors.Select(e => e.Description)));

        const string tokenHash = "test-refresh-token-hash";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        await fixture.IdentityService.StoreRefreshTokenAsync(
            user.Id,
            tokenHash,
            expiresAt);

        // Assert
        var storedToken = await fixture.DbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash);

        Assert.NotNull(storedToken);
        Assert.Equal(user.Id, storedToken.UserId);
        Assert.Equal(tokenHash, storedToken.TokenHash);
        Assert.Equal(expiresAt, storedToken.ExpiresAt);
        Assert.Null(storedToken.RevokedAt);
        Assert.Null(storedToken.ReplacedByTokenHash);
    }

    [Fact]
    public async Task StoreRefreshTokenAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        // Arrange
        await using var fixture =
            await IdentityServiceTestFixture.CreateAsync();

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => fixture.IdentityService.StoreRefreshTokenAsync(
                "non-existing-user-id",
                "test-refresh-token-hash",
                DateTime.UtcNow.AddDays(7)));
    }


    [Fact]
    public async Task RotateRefreshTokenAsync_ShouldRotateToken_WhenTokenIsValid()
    {
        await using var fixture = await IdentityServiceTestFixture.CreateAsync();

        // Arrange: Create a user
        var user = new ApplicationUser
        {
            FirstName = "Mohammad",
            LastName = "Moradi",
            Email = "mohammad@example.com",
            UserName = "mohammad@example.com"
        };

        var createResult = await fixture.UserManager.CreateAsync(
            user,
            "Password123!");

        Assert.True(
            createResult.Succeeded,
            string.Join(", ", createResult.Errors.Select(e => e.Description)));

        // Arrange: Store the original refresh token
        const string oldTokenHash = "old-refresh-token-hash";
        const string newTokenHash = "new-refresh-token-hash";

        await fixture.IdentityService.StoreRefreshTokenAsync(
            user.Id,
            oldTokenHash,
            DateTime.UtcNow.AddDays(7));

        var newExpiresAt = DateTime.UtcNow.AddDays(7);

        // Act: Rotate the refresh token
        var result = await fixture.IdentityService.RotateRefreshTokenAsync(
            oldTokenHash,
            newTokenHash,
            newExpiresAt);

        // Assert: Operation succeeded
        Assert.True(result.Succeeded);
        Assert.False(result.ReuseDetected);
        Assert.Equal(user.Id, result.UserId);

        // Assert: Old token was revoked
        var oldToken = await fixture.DbContext.RefreshTokens
            .SingleAsync(x => x.TokenHash == oldTokenHash);

        Assert.NotNull(oldToken.RevokedAt);
        Assert.Equal(newTokenHash, oldToken.ReplacedByTokenHash);

        // Assert: New token was persisted and remains active
        var newToken = await fixture.DbContext.RefreshTokens
            .SingleAsync(x => x.TokenHash == newTokenHash);

        Assert.Equal(user.Id, newToken.UserId);
        Assert.Equal(newExpiresAt, newToken.ExpiresAt);
        Assert.Null(newToken.RevokedAt);
        Assert.Null(newToken.ReplacedByTokenHash);
    }


    [Fact]
    public async Task RotateRefreshTokenAsync_ShouldFail_WhenTokenIsExpired()
    {
        await using var fixture = await IdentityServiceTestFixture.CreateAsync();

        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "Mohammad",
            LastName = "Moradi",
            Email = "mohammad@example.com",
            UserName = "mohammad@example.com"
        };

        var createResult = await fixture.UserManager.CreateAsync(
            user,
            "Password123!");

        Assert.True(
            createResult.Succeeded,
            string.Join(", ", createResult.Errors.Select(e => e.Description)));

        const string oldTokenHash = "expired-refresh-token-hash";
        const string newTokenHash = "new-refresh-token-hash";

        await fixture.IdentityService.StoreRefreshTokenAsync(
            user.Id,
            oldTokenHash,
            DateTime.UtcNow.AddMinutes(-5));

        // Act
        var result = await fixture.IdentityService.RotateRefreshTokenAsync(
            oldTokenHash,
            newTokenHash,
            DateTime.UtcNow.AddDays(7));

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.ReuseDetected);

        Assert.Contains(
            result.Errors,
            error => error.Code == "RefreshTokenExpired");

        var oldToken = await fixture.DbContext.RefreshTokens
            .SingleAsync(x => x.TokenHash == oldTokenHash);

        Assert.Null(oldToken.RevokedAt);
        Assert.Null(oldToken.ReplacedByTokenHash);

        var newToken = await fixture.DbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == newTokenHash);

        Assert.Null(newToken);
    }


    [Fact]
    public async Task RotateRefreshTokenAsync_ShouldDetectReuse_WhenTokenWasAlreadyRevoked()
    {
        await using var fixture = await IdentityServiceTestFixture.CreateAsync();

        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "Mohammad",
            LastName = "Moradi",
            Email = "mohammad@example.com",
            UserName = "mohammad@example.com"
        };

        var createResult = await fixture.UserManager.CreateAsync(
            user,
            "Password123!");

        Assert.True(
            createResult.Succeeded,
            string.Join(", ", createResult.Errors.Select(e => e.Description)));

        const string oldTokenHash = "revoked-refresh-token-hash";
        const string replacementTokenHash = "replacement-refresh-token-hash";

        await fixture.IdentityService.StoreRefreshTokenAsync(
            user.Id,
            oldTokenHash,
            DateTime.UtcNow.AddDays(7));

        // Simulate a token that has already been used.
        var oldToken = await fixture.DbContext.RefreshTokens
            .SingleAsync(x => x.TokenHash == oldTokenHash);

        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.ReplacedByTokenHash = replacementTokenHash;

        await fixture.DbContext.SaveChangesAsync();

        // Act
        var result = await fixture.IdentityService.RotateRefreshTokenAsync(
            oldTokenHash,
            "another-new-token-hash",
            DateTime.UtcNow.AddDays(7));

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.ReuseDetected);

        Assert.Contains(
            result.Errors,
            error => error.Code == "RefreshTokenReuseDetected");

        var persistedOldToken = await fixture.DbContext.RefreshTokens
            .SingleAsync(x => x.TokenHash == oldTokenHash);

        Assert.NotNull(persistedOldToken.RevokedAt);
        Assert.Equal(
            replacementTokenHash,
            persistedOldToken.ReplacedByTokenHash);

        var unexpectedNewToken = await fixture.DbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == "another-new-token-hash");

        Assert.Null(unexpectedNewToken);
    }


    [Fact]
    public async Task RotateRefreshTokenAsync_ShouldRevokeAllActiveTokens_WhenReuseIsDetected()
    {
        await using var fixture = await IdentityServiceTestFixture.CreateAsync();

        // Arrange: Create user
        var user = new ApplicationUser
        {
            FirstName = "Mohammad",
            LastName = "Moradi",
            Email = "mohammad@example.com",
            UserName = "mohammad@example.com"
        };

        var createResult = await fixture.UserManager.CreateAsync(
            user,
            "Password123!");

        Assert.True(
            createResult.Succeeded,
            string.Join(", ", createResult.Errors.Select(e => e.Description)));

        const string oldTokenHash = "old-refresh-token-hash";
        const string activeTokenHash1 = "active-refresh-token-hash-1";
        const string activeTokenHash2 = "active-refresh-token-hash-2";

        await fixture.IdentityService.StoreRefreshTokenAsync(
            user.Id,
            oldTokenHash,
            DateTime.UtcNow.AddDays(7));

        await fixture.IdentityService.StoreRefreshTokenAsync(
            user.Id,
            activeTokenHash1,
            DateTime.UtcNow.AddDays(7));

        await fixture.IdentityService.StoreRefreshTokenAsync(
            user.Id,
            activeTokenHash2,
            DateTime.UtcNow.AddDays(7));

        // Simulate previous use of the old token
        var oldToken = await fixture.DbContext.RefreshTokens
            .SingleAsync(x => x.TokenHash == oldTokenHash);

        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.ReplacedByTokenHash = activeTokenHash1;

        await fixture.DbContext.SaveChangesAsync();

        // Act: Reuse the revoked token
        var result = await fixture.IdentityService.RotateRefreshTokenAsync(
            oldTokenHash,
            "unexpected-new-token-hash",
            DateTime.UtcNow.AddDays(7));

        // Assert: Reuse detected
        Assert.False(result.Succeeded);
        Assert.True(result.ReuseDetected);

        Assert.Contains(
            result.Errors,
            error => error.Code == "RefreshTokenReuseDetected");

        // Assert: All active tokens have been revoked
        var userTokens = await fixture.DbContext.RefreshTokens
            .Where(x => x.UserId == user.Id)
            .ToListAsync();

        Assert.All(
            userTokens,
            token => Assert.NotNull(token.RevokedAt));

        // Assert: Existing replacement relationship remains unchanged
        var persistedOldToken = userTokens
            .Single(x => x.TokenHash == oldTokenHash);

        Assert.Equal(
            activeTokenHash1,
            persistedOldToken.ReplacedByTokenHash);

        // Assert: No unexpected token was created
        Assert.DoesNotContain(
            userTokens,
            token => token.TokenHash == "unexpected-new-token-hash");
    }

}
