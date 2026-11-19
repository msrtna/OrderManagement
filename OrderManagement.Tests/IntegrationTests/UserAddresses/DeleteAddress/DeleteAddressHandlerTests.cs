
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.UserAddresses.DeleteAddress;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.UserAddresses.DeleteAddress;

public sealed class DeleteAddressHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private DeleteAddressHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        await SeedUserAsync("user-1", "testuser");
        await SeedUserAsync("user-2", "anotheruser");

        _handler = new DeleteAddressHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteAddress_WhenAddressBelongsToCurrentUser()
    {
        var address = await SeedAddressAsync("user-1", "Home", false);

        var result = await _handler.HandleAsync(address.Id);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(address.Id, result.AddressId);

        Assert.False(await _context.UserAddresses
            .AnyAsync(item => item.Id == address.Id));
    }

    [Fact]
    public async Task HandleAsync_ShouldSetReplacementAsDefault_WhenDeletingDefaultAddress()
    {
        var defaultAddress = await SeedAddressAsync("user-1", "Home", true);
        var replacementAddress = await SeedAddressAsync("user-1", "Work", false);

        var result = await _handler.HandleAsync(defaultAddress.Id);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);

        Assert.False(await _context.UserAddresses
            .AnyAsync(item => item.Id == defaultAddress.Id));

        var remainingAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == replacementAddress.Id);

        Assert.True(remainingAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldSelectOldestRemainingAddress_WhenDeletingDefaultAddress()
    {
        var defaultAddress = await SeedAddressAsync("user-1", "Home", true);
        var oldestAddress = await SeedAddressAsync("user-1", "Work", false);
        var newestAddress = await SeedAddressAsync("user-1", "Office", false);

        var result = await _handler.HandleAsync(defaultAddress.Id);

        Assert.NotNull(result);

        var addresses = await _context.UserAddresses
            .Where(item => item.UserId == "user-1")
            .ToListAsync();

        Assert.True(addresses.Single(item => item.Id == oldestAddress.Id).IsDefault);
        Assert.False(addresses.Single(item => item.Id == newestAddress.Id).IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotChangeDefaultAddress_WhenDeletingNonDefaultAddress()
    {
        var defaultAddress = await SeedAddressAsync("user-1", "Home", true);
        var otherAddress = await SeedAddressAsync("user-1", "Work", false);

        var result = await _handler.HandleAsync(otherAddress.Id);

        Assert.NotNull(result);

        var storedDefaultAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == defaultAddress.Id);

        Assert.True(storedDefaultAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteOnlyAddress_WithoutCreatingReplacement()
    {
        var address = await SeedAddressAsync("user-1", "Home", true);

        var result = await _handler.HandleAsync(address.Id);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);

        Assert.Empty(await _context.UserAddresses
            .Where(item => item.UserId == "user-1")
            .ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotChangeOtherUsersAddresses()
    {
        var otherUsersAddress = await SeedAddressAsync("user-2", "Other Home", true);
        var ownAddress = await SeedAddressAsync("user-1", "Home", false);

        var result = await _handler.HandleAsync(ownAddress.Id);

        Assert.NotNull(result);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == otherUsersAddress.Id);

        Assert.True(storedAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenAddressDoesNotExist()
    {
        var result = await _handler.HandleAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenAddressBelongsToAnotherUser()
    {
        var address = await SeedAddressAsync("user-2", "Other Home", true);

        var result = await _handler.HandleAsync(address.Id);

        Assert.Null(result);

        Assert.True(await _context.UserAddresses
            .AnyAsync(item => item.Id == address.Id));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        var address = await SeedAddressAsync("user-1", "Home", true);

        var unauthenticatedHandler = new DeleteAddressHandler(
            _context,
            new FakeCurrentUserService(null));

        var result = await unauthenticatedHandler.HandleAsync(address.Id);

        Assert.Null(result);

        Assert.True(await _context.UserAddresses
            .AnyAsync(item => item.Id == address.Id));
    }

    private async Task SeedUserAsync(string userId, string userName)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = userName,
            Email = $"{userName}@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    private async Task<UserAddress> SeedAddressAsync(
        string userId,
        string title,
        bool isDefault)
    {
        var address = new UserAddress
        {
            UserId = userId,
            Title = title,
            RecipientName = "Test User",
            PhoneNumber = "09123456789",
            Province = "Tehran",
            City = "Tehran",
            AddressLine = "Main Street, No. 10",
            PostalCode = "1234567890",
            IsDefault = isDefault
        };

        await _context.UserAddresses.AddAsync(address);
        await _context.SaveChangesAsync();

        return address;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; }

        public FakeCurrentUserService(string? userId)
        {
            UserId = userId;
        }
    }
}
