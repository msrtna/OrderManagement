using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.ShoppingCarts.RemoveCartItem;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.ShoppingCarts.RemoveCartItem;

public sealed class RemoveCartItemHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private RemoveCartItemHandler _handler = null!;

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

        _handler = new RemoveCartItemHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveCartItem_WhenItemBelongsToCurrentUser()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(
            product,
            "user-1",
            2);

        var result = await _handler.HandleAsync(cartItem.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(cartItem.Id, result.Response.CartItemId);
        Assert.Equal(product.Name, result.Response.ProductName);
        Assert.Empty(result.Errors);

        Assert.False(
            await _context.CartItems.AnyAsync(
                item => item.Id == cartItem.Id));
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCartItemDoesNotExist()
    {
        var result = await _handler.HandleAsync(999);

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

        var result = await _handler.HandleAsync(cartItem.Id);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "CartItemNotFound");

        Assert.True(
            await _context.CartItems.AnyAsync(
                item => item.Id == cartItem.Id));
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(
            product,
            "user-1",
            2);

        var unauthenticatedHandler = new RemoveCartItemHandler(
            _context,
            new FakeCurrentUserService(null));

        var result = await unauthenticatedHandler.HandleAsync(
            cartItem.Id);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "UserNotAuthenticated");

        Assert.True(
            await _context.CartItems.AnyAsync(
                item => item.Id == cartItem.Id));
    }

    [Fact]
    public async Task HandleAsync_ShouldNotDeleteProduct_WhenCartItemIsRemoved()
    {
        var product = await SeedProductAsync();
        var cartItem = await SeedCartItemAsync(
            product,
            "user-1",
            2);

        var result = await _handler.HandleAsync(cartItem.Id);

        Assert.True(result.Succeeded);

        var storedProduct = await _context.Products
            .SingleOrDefaultAsync(
                item => item.Id == product.Id);

        Assert.NotNull(storedProduct);
        Assert.Equal(product.Name, storedProduct.Name);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepOtherCartItems_WhenOneItemIsRemoved()
    {
        var product1 = await SeedProductAsync("Laptop");
        var product2 = await SeedProductAsync("Mouse");

        var cart = await SeedCartAsync("user-1");

        var item1 = await SeedCartItemAsync(product1, cart.Id, 2);
        var item2 = await SeedCartItemAsync(product2, cart.Id, 3);

        var result = await _handler.HandleAsync(item1.Id);

        Assert.True(result.Succeeded);

        var remainingItems = await _context.CartItems
            .Where(item => item.ShoppingCartId == cart.Id)
            .ToListAsync();

        Assert.Single(remainingItems);
        Assert.Equal(item2.Id, remainingItems[0].Id);
        Assert.Equal("Mouse", remainingItems[0].ProductName);
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
        string userId,
        int quantity)
    {
        var cart = await SeedCartAsync(userId);

        return await SeedCartItemAsync(
            product,
            cart.Id,
            quantity);
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
