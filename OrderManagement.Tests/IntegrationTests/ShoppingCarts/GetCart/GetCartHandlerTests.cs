using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.ShoppingCarts.GetCart;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.ShoppingCarts.GetCart;

public sealed class GetCartHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetCartHandler _handler = null!;

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
        await SeedUserAsync();

        var currentUserService = new FakeCurrentUserService("user-1");

        _handler = new GetCartHandler(
            _context,
            currentUserService);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCartWithItems_WhenCartExists()
    {
        var product = await SeedProductAsync();

        await SeedCartAsync();

        var cart = await _context.ShoppingCarts.SingleAsync();

        await _context.CartItems.AddAsync(new CartItem
        {
            ShoppingCartId = cart.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = 2,
            IsProductDeleted = false
        });

        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Equal(cart.Id, result.ShoppingCartId);
        Assert.Single(result.Items);

        var item = result.Items.Single();

        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal("Laptop", item.ProductName);
        Assert.Equal(1500m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(3000m, item.LineTotal);
        Assert.False(item.IsProductDeleted);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(3000m, result.TotalAmount);
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateTotalAmount_WhenCartHasMultipleItems()
    {
        var laptop = await SeedProductAsync();

        var phone = new Product
        {
            Name = "Phone",
            Description = "Smart Phone",
            Price = 800,
            StockQuantity = 20,
            CategoryId = 1,
            IsActive = true
        };

        await _context.Products.AddAsync(phone);
        await _context.SaveChangesAsync();

        var cart = await SeedCartAsync();

        await _context.CartItems.AddRangeAsync(
            new CartItem
            {
                ShoppingCartId = cart.Id,
                ProductId = laptop.Id,
                ProductName = laptop.Name,
                Quantity = 2
            },
            new CartItem
            {
                ShoppingCartId = cart.Id,
                ProductId = phone.Id,
                ProductName = phone.Name,
                Quantity = 3
            });

        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(5400m, result.TotalAmount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCart_WhenCartDoesNotExist()
    {
        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Equal(0, result.ShoppingCartId);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnZeroPrice_WhenProductIsInactive()
    {
        var product = await SeedProductAsync();

        product.IsActive = false;
        await _context.SaveChangesAsync();

        var cart = await SeedCartAsync();

        await _context.CartItems.AddAsync(new CartItem
        {
            ShoppingCartId = cart.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = 2,
            IsProductDeleted = false
        });

        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);

        var item = result.Items.Single();

        Assert.Equal("Laptop", item.ProductName);
        Assert.Equal(0m, item.UnitPrice);
        Assert.Equal(0m, item.LineTotal);
        Assert.True(item.IsProductDeleted);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDeletedProductItem_WhenProductIsDeleted()
    {
        var cart = await SeedCartAsync();

        await _context.CartItems.AddAsync(new CartItem
        {
            ShoppingCartId = cart.Id,
            ProductId = null,
            ProductName = "Deleted Laptop",
            Quantity = 1,
            IsProductDeleted = true
        });

        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);

        var item = result.Items.Single();

        Assert.Null(item.ProductId);
        Assert.Equal("Deleted Laptop", item.ProductName);
        Assert.Equal(0m, item.UnitPrice);
        Assert.Equal(1, item.Quantity);
        Assert.True(item.IsProductDeleted);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        var unauthenticatedHandler = new GetCartHandler(
            _context,
            new FakeCurrentUserService(null));

        var result = await unauthenticatedHandler.HandleAsync();

        Assert.Null(result);
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

    private async Task SeedUserAsync()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "testuser",
            Email = "test@example.com",
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

    private async Task<ShoppingCart> SeedCartAsync()
    {
        var cart = new ShoppingCart
        {
            UserId = "user-1"
        };

        await _context.ShoppingCarts.AddAsync(cart);
        await _context.SaveChangesAsync();

        return cart;
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
