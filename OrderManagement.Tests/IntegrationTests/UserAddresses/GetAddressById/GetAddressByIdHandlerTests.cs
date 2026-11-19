
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.UserAddresses.GetAddressById;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.UserAddresses.GetAddressById;

public sealed class GetAddressByIdHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetAddressByIdHandler _handler = null!;

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

        _handler = new GetAddressByIdHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAddress_WhenAddressBelongsToCurrentUser()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var result = await _handler.HandleAsync(address.Id);

        Assert.NotNull(result);

        Assert.Equal(address.Id, result.AddressId);
        Assert.Equal("Home", result.Title);
        Assert.Equal("Test User", result.RecipientName);
        Assert.Equal("09123456789", result.PhoneNumber);
        Assert.Equal("Tehran", result.Province);
        Assert.Equal("Tehran", result.City);
        Assert.Equal("Main Street, No. 10", result.AddressLine);
        Assert.Equal("1234567890", result.PostalCode);
        Assert.True(result.IsDefault);
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
        var otherUsersAddress = await SeedAddressAsync(
            "user-2",
            "Other User Home",
            true);

        var result = await _handler.HandleAsync(otherUsersAddress.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var unauthenticatedHandler = new GetAddressByIdHandler(
            _context,
            new FakeCurrentUserService(null));

        var result = await unauthenticatedHandler.HandleAsync(address.Id);

        Assert.Null(result);
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
