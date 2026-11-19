
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.UserAddresses.SetDefaultAddress;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.UserAddresses.SetDefaultAddress;

public sealed class SetDefaultAddressHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private SetDefaultAddressHandler _handler = null!;

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

        _handler = new SetDefaultAddressHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task HandleAsync_ShouldSetAddressAsDefault_WhenAddressBelongsToCurrentUser()
    {
        await SeedAddressAsync("user-1", "Home", false);

        var address = await SeedAddressAsync(
            "user-1",
            "Work",
            false);

        var result = await _handler.HandleAsync(address.Id);

        Assert.NotNull(result);
        Assert.Equal(address.Id, result.AddressId);
        Assert.True(result.IsDefault);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.True(storedAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldReplaceExistingDefaultAddress()
    {
        var existingDefaultAddress = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var newDefaultAddress = await SeedAddressAsync(
            "user-1",
            "Work",
            false);

        var result = await _handler.HandleAsync(newDefaultAddress.Id);

        Assert.NotNull(result);
        Assert.True(result.IsDefault);

        var addresses = await _context.UserAddresses
            .Where(item => item.UserId == "user-1")
            .ToListAsync();

        Assert.Equal(2, addresses.Count);

        Assert.False(
            addresses.Single(item => item.Id == existingDefaultAddress.Id)
                .IsDefault);

        Assert.True(
            addresses.Single(item => item.Id == newDefaultAddress.Id)
                .IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnExistingDefault_WhenAddressIsAlreadyDefault()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var result = await _handler.HandleAsync(address.Id);

        Assert.NotNull(result);
        Assert.Equal(address.Id, result.AddressId);
        Assert.True(result.IsDefault);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.True(storedAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotChangeOtherUsersDefaultAddress()
    {
        await SeedAddressAsync(
            "user-2",
            "Other User Home",
            true);

        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            false);

        var result = await _handler.HandleAsync(address.Id);

        Assert.NotNull(result);

        var otherUsersAddress = await _context.UserAddresses
            .SingleAsync(item => item.UserId == "user-2");

        Assert.True(otherUsersAddress.IsDefault);
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
        var address = await SeedAddressAsync(
            "user-2",
            "Other User Home",
            true);

        var result = await _handler.HandleAsync(address.Id);

        Assert.Null(result);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.True(storedAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            false);

        var unauthenticatedHandler = new SetDefaultAddressHandler(
            _context,
            new FakeCurrentUserService(null));

        var result = await unauthenticatedHandler.HandleAsync(address.Id);

        Assert.Null(result);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.False(storedAddress.IsDefault);
    }

    private async Task SeedUserAsync(
        string userId,
        string userName)
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
