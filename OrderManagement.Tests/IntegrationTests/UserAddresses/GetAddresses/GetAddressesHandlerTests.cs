
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.UserAddresses.GetAddresses;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.UserAddresses.GetAddresses;

public sealed class GetAddressesHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetAddressesHandler _handler = null!;

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

        _handler = new GetAddressesHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnOnlyCurrentUsersAddresses()
    {
        await SeedAddressAsync("user-1", "Home", true);
        await SeedAddressAsync("user-1", "Work", false);
        await SeedAddressAsync("user-2", "Other User Home", true);

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Addresses.Count);

        Assert.All(
            result.Addresses,
            address => Assert.Contains(
                address.Title,
                new[] { "Home", "Work" }));

        Assert.DoesNotContain(
            result.Addresses,
            address => address.Title == "Other User Home");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDefaultAddressFirst()
    {
        await SeedAddressAsync("user-1", "Home", false);
        await SeedAddressAsync("user-1", "Work", false);
        await SeedAddressAsync("user-1", "Default", true);

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Equal(3, result.Addresses.Count);

        Assert.Equal("Default", result.Addresses.First().Title);
        Assert.True(result.Addresses.First().IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAddressesOrderedById()
    {
        var firstAddress = await SeedAddressAsync(
            "user-1",
            "First",
            false);

        var secondAddress = await SeedAddressAsync(
            "user-1",
            "Second",
            false);

        var thirdAddress = await SeedAddressAsync(
            "user-1",
            "Third",
            false);

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);

        Assert.Collection(
            result.Addresses,
            address => Assert.Equal(firstAddress.Id, address.AddressId),
            address => Assert.Equal(secondAddress.Id, address.AddressId),
            address => Assert.Equal(thirdAddress.Id, address.AddressId));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenUserHasNoAddresses()
    {
        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Empty(result.Addresses);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        var unauthenticatedHandler = new GetAddressesHandler(
            _context,
            new FakeCurrentUserService(null));

        var result = await unauthenticatedHandler.HandleAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllAddressFields()
    {
        await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);

        var address = Assert.Single(result.Addresses);

        Assert.Equal("Home", address.Title);
        Assert.Equal("Test User", address.RecipientName);
        Assert.Equal("09123456789", address.PhoneNumber);
        Assert.Equal("Tehran", address.Province);
        Assert.Equal("Tehran", address.City);
        Assert.Equal("Main Street, No. 10", address.AddressLine);
        Assert.Equal("1234567890", address.PostalCode);
        Assert.True(address.IsDefault);
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
