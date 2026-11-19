
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.ShoppingCarts.UpdateCartItemQuantity;

public sealed class UpdateCartItemQuantityHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private UpdateCartItemQuantityHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        await SeedCategoryAsync();
        await SeedUserAsync("user-1", "testuser");
        await SeedUserAsync("user-2", "anotheruser");

        _handler = new UpdateCartItemQuantityHandler(
            _context,
            new FakeCurrentUserService("user-1"),
            new UpdateCartItemQuantityValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateQuantity_WhenRequestIsValid()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(product, "user-1", 2);

        var result = await _handler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(5));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(cartItem.Id, result.Response.CartItemId);
        Assert.Equal(product.Id, result.Response.ProductId);
        Assert.Equal(product.Name, result.Response.ProductName);
        Assert.Equal(5, result.Response.Quantity);

        var updatedItem = await _context.CartItems.SingleAsync();

        Assert.Equal(5, updatedItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCartItemDoesNotExist()
    {
        var result = await _handler.HandleAsync(
            999,
            new UpdateCartItemQuantityRequest(3));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "CartItemNotFound");
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCartItemBelongsToAnotherUser()
    {
        var product = await SeedProductAsync();

        var cartItem = await SeedCartItemAsync(
            product,
            "user-2",
            2);

        var result = await _handler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(5));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "CartItemNotFound");

        var storedItem = await _context.CartItems.SingleAsync();

        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(product, "user-1", 2);

        var result = await _handler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(0));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.NotEmpty(result.Errors);

        var storedItem = await _context.CartItems.SingleAsync();

        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenMaximumQuantityIsExceeded()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(product, "user-1", 2);

        var settings = await _context.StoreSettings.SingleAsync(
            x => x.Id == 1);

        settings.MaxCartItemQuantity = 3;

        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(4));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "MaximumCartItemQuantityExceeded");

        var storedItem = await _context.CartItems.SingleAsync();

        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductIsInactive()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(product, "user-1", 2);

        product.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(3));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotAvailable");

        Assert.Equal(2, cartItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductIsDeleted()
    {
        var cartItem = await SeedDeletedProductCartItemAsync("user-1");

        var result = await _handler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(3));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotAvailable");

        Assert.Equal(2, cartItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(product, "user-1", 2);

        var unauthenticatedHandler = new UpdateCartItemQuantityHandler(
            _context,
            new FakeCurrentUserService(null),
            new UpdateCartItemQuantityValidator());

        var result = await unauthenticatedHandler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(3));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "UserNotAuthenticated");

        Assert.Equal(2, cartItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenStoreSettingsDoNotExist()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(product, "user-1", 2);

        var settings = await _context.StoreSettings.SingleAsync(
            x => x.Id == 1);

        _context.StoreSettings.Remove(settings);
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            cartItem.Id,
            new UpdateCartItemQuantityRequest(3));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "StoreSettingsNotFound");

        Assert.Equal(2, cartItem.Quantity);
    }

    private async Task SeedCategoryAsync()
    {
        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic products",
            IsActive = true
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
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

    private async Task<Product> SeedProductAsync()
    {
        var product = new Product
        {
            Name = "Laptop",
            Description = "Gaming Laptop",
            Price = 1500,
            StockQuantity = 10,
            CategoryId = 1,
            IsActive = true
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return product;
    }

    private async Task<CartItem> SeedCartItemAsync(
        Product product,
        string userId,
        int quantity)
    {
        var cart = new ShoppingCart
        {
            UserId = userId
        };

        await _context.ShoppingCarts.AddAsync(cart);
        await _context.SaveChangesAsync();

        var cartItem = new CartItem
        {
            ShoppingCartId = cart.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            IsProductDeleted = false
        };

        await _context.CartItems.AddAsync(cartItem);
        await _context.SaveChangesAsync();

        return cartItem;
    }

    private async Task<CartItem> SeedDeletedProductCartItemAsync(
        string userId)
    {
        var cart = new ShoppingCart
        {
            UserId = userId
        };

        await _context.ShoppingCarts.AddAsync(cart);
        await _context.SaveChangesAsync();

        var cartItem = new CartItem
        {
            ShoppingCartId = cart.Id,
            ProductId = null,
            ProductName = "Deleted Laptop",
            Quantity = 2,
            IsProductDeleted = true
        };

        await _context.CartItems.AddAsync(cartItem);
        await _context.SaveChangesAsync();

        return cartItem;
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
