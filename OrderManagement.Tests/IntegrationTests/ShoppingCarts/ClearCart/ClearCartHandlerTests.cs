using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.ShoppingCarts.ClearCart;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.ShoppingCarts.ClearCart;

public sealed class ClearCartHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private ClearCartHandler _handler = null!;

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

        _handler = new ClearCartHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveAllItems_WhenCartContainsItems()
    {
        var product1 = await SeedProductAsync("Laptop");
        var product2 = await SeedProductAsync("Mouse");

        var cart = await SeedCartAsync("user-1");

        await SeedCartItemAsync(product1, cart.Id, 2);
        await SeedCartItemAsync(product2, cart.Id, 3);

        var result = await _handler.HandleAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(2, result.Response.RemovedItemsCount);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);

        Assert.Empty(
            await _context.CartItems
                .Where(item => item.ShoppingCartId == cart.Id)
                .ToListAsync());

        Assert.True(
            await _context.ShoppingCarts.AnyAsync(
                item => item.Id == cart.Id));
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithZero_WhenCartIsEmpty()
    {
        await SeedCartAsync("user-1");

        var result = await _handler.HandleAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(0, result.Response.RemovedItemsCount);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithZero_WhenCartDoesNotExist()
    {
        var result = await _handler.HandleAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(0, result.Response.RemovedItemsCount);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRemoveAnotherUsersCartItems()
    {
        var product = await SeedProductAsync();

        var otherUsersCart = await SeedCartAsync("user-2");

        var cartItem = await SeedCartItemAsync(
            product,
            otherUsersCart.Id,
            2);

        var result = await _handler.HandleAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(0, result.Response.RemovedItemsCount);

        Assert.True(
            await _context.CartItems.AnyAsync(
                item => item.Id == cartItem.Id));
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var product = await SeedProductAsync();

        var cart = await SeedCartAsync("user-1");

        var cartItem = await SeedCartItemAsync(
            product,
            cart.Id,
            2);

        var unauthenticatedHandler = new ClearCartHandler(
            _context,
            new FakeCurrentUserService(null));

        var result = await unauthenticatedHandler.HandleAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.Equal(
            "UserNotAuthenticated",
            result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);

        Assert.True(
            await _context.CartItems.AnyAsync(
                item => item.Id == cartItem.Id));
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

    private async Task<Product> SeedProductAsync(
        string name = "Laptop")
    {
        var product = new Product
        {
            Name = name,
            Description = $"{name} description",
            Price = 1500,
            StockQuantity = 10,
            CategoryId = 1,
            IsActive = true
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return product;
    }

    private async Task<ShoppingCart> SeedCartAsync(string userId)
    {
        var cart = new ShoppingCart
        {
            UserId = userId
        };

        await _context.ShoppingCarts.AddAsync(cart);
        await _context.SaveChangesAsync();

        return cart;
    }

    private async Task<CartItem> SeedCartItemAsync(
        Product product,
        long cartId,
        int quantity)
    {
        var cartItem = new CartItem
        {
            ShoppingCartId = cartId,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            IsProductDeleted = false
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
