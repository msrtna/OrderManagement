
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.UserAddresses.CreateAddress;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.UserAddresses.CreateAddress;

public sealed class CreateAddressHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private CreateAddressHandler _handler = null!;

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

        _handler = new CreateAddressHandler(
            _context,
            new FakeCurrentUserService("user-1"),
            new CreateAddressValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateFirstAddressAsDefault()
    {
        var result = await _handler.HandleAsync(
            CreateRequest(isDefault: false));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Empty(result.Errors);

        Assert.True(result.Response.IsDefault);
        Assert.Equal("Home", result.Response.Title);

        var storedAddress = await _context.UserAddresses
            .SingleAsync();

        Assert.Equal("user-1", storedAddress.UserId);
        Assert.True(storedAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepExistingDefault_WhenNewAddressIsNotDefault()
    {
        var existingAddress = await SeedAddressAsync(
            "user-1",
            "Home",
            isDefault: true);

        var result = await _handler.HandleAsync(
            CreateRequest(
                title: "Work",
                isDefault: false));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.False(result.Response.IsDefault);

        var addresses = await _context.UserAddresses
            .OrderBy(address => address.Id)
            .ToListAsync();

        Assert.Equal(2, addresses.Count);

        Assert.True(
            addresses.Single(address => address.Id == existingAddress.Id)
                .IsDefault);

        Assert.False(
            addresses.Single(address => address.Id != existingAddress.Id)
                .IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldReplaceExistingDefault_WhenNewAddressIsDefault()
    {
        var existingAddress = await SeedAddressAsync(
            "user-1",
            "Home",
            isDefault: true);

        var result = await _handler.HandleAsync(
            CreateRequest(
                title: "Work",
                isDefault: true));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.True(result.Response.IsDefault);

        var addresses = await _context.UserAddresses
            .ToListAsync();

        Assert.Equal(2, addresses.Count);

        Assert.False(
            addresses.Single(address => address.Id == existingAddress.Id)
                .IsDefault);

        Assert.True(
            addresses.Single(address => address.Id != existingAddress.Id)
                .IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotChangeOtherUsersDefaultAddress()
    {
        await SeedAddressAsync(
            "user-2",
            "Other User Home",
            isDefault: true);

        var result = await _handler.HandleAsync(
            CreateRequest(isDefault: true));

        Assert.True(result.Succeeded);

        var otherUsersAddress = await _context.UserAddresses
            .SingleAsync(address => address.UserId == "user-2");

        Assert.True(otherUsersAddress.IsDefault);

        var currentUsersAddress = await _context.UserAddresses
            .SingleAsync(address => address.UserId == "user-1");

        Assert.True(currentUsersAddress.IsDefault);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var request = CreateRequest() with
        {
            Title = "",
            City = ""
        };

        var result = await _handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.NotEmpty(result.Errors);

        Assert.Empty(await _context.UserAddresses.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var unauthenticatedHandler = new CreateAddressHandler(
            _context,
            new FakeCurrentUserService(null),
            new CreateAddressValidator());

        var result = await unauthenticatedHandler.HandleAsync(
            CreateRequest());

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "UserNotAuthenticated");

        Assert.Empty(await _context.UserAddresses.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldTrimTextFields_WhenCreatingAddress()
    {
        var request = CreateRequest() with
        {
            Title = "  Home  ",
            RecipientName = "  Test User  ",
            PhoneNumber = "  09123456789  ",
            Province = "  Tehran  ",
            City = "  Tehran  ",
            AddressLine = "  Main Street  ",
            PostalCode = "  1234567890  "
        };

        var result = await _handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("Home", result.Response.Title);
        Assert.Equal("Test User", result.Response.RecipientName);
        Assert.Equal("09123456789", result.Response.PhoneNumber);
        Assert.Equal("Tehran", result.Response.Province);
        Assert.Equal("Tehran", result.Response.City);
        Assert.Equal("Main Street", result.Response.AddressLine);
        Assert.Equal("1234567890", result.Response.PostalCode);
    }

    private static CreateAddressRequest CreateRequest(
        string title = "Home",
        bool isDefault = false)
    {
        return new CreateAddressRequest(
            title,
            "Test User",
            "09123456789",
            "Tehran",
            "Tehran",
            "Main Street, No. 10",
            "1234567890",
            isDefault);
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
            AddressLine = "Main Street",
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
