
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.UserAddresses.UpdateAddress;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.UserAddresses.UpdateAddress;

public sealed class UpdateAddressHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private UpdateAddressHandler _handler = null!;

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

        _handler = new UpdateAddressHandler(
            _context,
            new FakeCurrentUserService("user-1"),
            new UpdateAddressValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateAddress_WhenAddressBelongsToCurrentUser()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var request = CreateRequest(
            title: "Updated Home");

        var result = await _handler.HandleAsync(
            address.Id,
            request);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);

        Assert.Equal(address.Id, result.Response.AddressId);
        Assert.Equal("Updated Home", result.Response.Title);
        Assert.True(result.Response.IsDefault);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.Equal("Updated Home", storedAddress.Title);
        Assert.True(storedAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAddressNotFound_WhenAddressDoesNotExist()
    {
        var result = await _handler.HandleAsync(
            999,
            CreateRequest());

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.Equal("AddressNotFound", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAddressNotFound_WhenAddressBelongsToAnotherUser()
    {
        var address = await SeedAddressAsync(
            "user-2",
            "Other User Home",
            true);

        var result = await _handler.HandleAsync(
            address.Id,
            CreateRequest(title: "Hacked Address"));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.Equal("AddressNotFound", result.ErrorCode);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.Equal("Other User Home", storedAddress.Title);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var request = CreateRequest() with
        {
            Title = "",
            City = ""
        };

        var result = await _handler.HandleAsync(
            address.Id,
            request);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.Equal("ValidationFailed", result.ErrorCode);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.Equal("Home", storedAddress.Title);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var unauthenticatedHandler = new UpdateAddressHandler(
            _context,
            new FakeCurrentUserService(null),
            new UpdateAddressValidator());

        var result = await unauthenticatedHandler.HandleAsync(
            address.Id,
            CreateRequest());

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.Equal("UserNotAuthenticated", result.ErrorCode);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == address.Id);

        Assert.Equal("Home", storedAddress.Title);
    }

    [Fact]
    public async Task HandleAsync_ShouldTrimTextFields_WhenUpdatingAddress()
    {
        var address = await SeedAddressAsync(
            "user-1",
            "Home",
            false);

        var request = CreateRequest() with
        {
            Title = "  Updated Home  ",
            RecipientName = "  Updated User  ",
            PhoneNumber = "  09111111111  ",
            Province = "  Alborz  ",
            City = "  Karaj  ",
            AddressLine = "  New Street  ",
            PostalCode = "  9876543210  "
        };

        var result = await _handler.HandleAsync(
            address.Id,
            request);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("Updated Home", result.Response.Title);
        Assert.Equal("Updated User", result.Response.RecipientName);
        Assert.Equal("09111111111", result.Response.PhoneNumber);
        Assert.Equal("Alborz", result.Response.Province);
        Assert.Equal("Karaj", result.Response.City);
        Assert.Equal("New Street", result.Response.AddressLine);
        Assert.Equal("9876543210", result.Response.PostalCode);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepDefaultStatus_WhenUpdatingAddress()
    {
        var defaultAddress = await SeedAddressAsync(
            "user-1",
            "Home",
            true);

        var result = await _handler.HandleAsync(
            defaultAddress.Id,
            CreateRequest(title: "Updated Home"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.True(result.Response.IsDefault);

        var storedAddress = await _context.UserAddresses
            .SingleAsync(item => item.Id == defaultAddress.Id);

        Assert.True(storedAddress.IsDefault);
    }

    private static UpdateAddressRequest CreateRequest(
        string title = "Updated Home")
    {
        return new UpdateAddressRequest(
            title,
            "Test User",
            "09123456789",
            "Tehran",
            "Tehran",
            "Main Street, No. 20",
            "1234567890");
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
