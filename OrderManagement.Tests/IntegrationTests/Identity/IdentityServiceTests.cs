using Microsoft.EntityFrameworkCore;

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
}
