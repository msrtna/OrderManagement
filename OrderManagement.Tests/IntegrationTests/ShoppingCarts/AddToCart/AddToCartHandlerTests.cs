using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.ShoppingCarts.AddToCart;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.ShoppingCarts.AddToCart;

public sealed class AddToCartHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private AddToCartHandler _handler = null!;

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

        var validator = new AddToCartValidator();

        _handler = new AddToCartHandler(
            _context,
            currentUserService,
            validator);
    }

    [Fact]
    public async Task HandleAsync_ShouldAddProductToNewCart_WhenRequestIsValid()
    {
        var product = await SeedProductAsync();

        var request = new AddToCartRequest(product.Id, 2);

        var result = await _handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(product.Id, result.Response.ProductId);
        Assert.Equal(product.Name, result.Response.ProductName);
        Assert.Equal(2, result.Response.Quantity);

        var cart = await _context.ShoppingCarts
            .Include(x => x.Items)
            .SingleAsync();

        Assert.Equal("user-1", cart.UserId);
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items.First().Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncreaseQuantity_WhenProductAlreadyExistsInCart()
    {
        var product = await SeedProductAsync();

        await _handler.HandleAsync(
            new AddToCartRequest(product.Id, 2));

        var result = await _handler.HandleAsync(
            new AddToCartRequest(product.Id, 3));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(5, result.Response.Quantity);

        var cartItems = await _context.CartItems.ToListAsync();

        Assert.Single(cartItems);
        Assert.Equal(5, cartItems[0].Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductDoesNotExist()
    {
        var result = await _handler.HandleAsync(
            new AddToCartRequest(999, 1));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotFoundOrInactive");

        Assert.Empty(await _context.ShoppingCarts.ToListAsync());
        Assert.Empty(await _context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductIsInactive()
    {
        var product = await SeedProductAsync();

        product.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            new AddToCartRequest(product.Id, 1));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotFoundOrInactive");

        Assert.Empty(await _context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var unauthenticatedHandler = new AddToCartHandler(
            _context,
            new FakeCurrentUserService(null),
            new AddToCartValidator());

        var product = await SeedProductAsync();

        var result = await unauthenticatedHandler.HandleAsync(
            new AddToCartRequest(product.Id, 1));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "UserNotAuthenticated");

        Assert.Empty(await _context.ShoppingCarts.ToListAsync());
        Assert.Empty(await _context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenMaximumQuantityIsExceeded()
    {
        var product = await SeedProductAsync();

        var settings = await _context.StoreSettings.SingleAsync(
            x => x.Id == 1);

        settings.MaxCartItemQuantity = 2;

        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            new AddToCartRequest(product.Id, 3));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "MaximumCartItemQuantityExceeded");

        Assert.Empty(await _context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenAddingQuantityExceedsMaximum()
    {
        var product = await SeedProductAsync();

        var settings = await _context.StoreSettings.SingleAsync(
            x => x.Id == 1);

        settings.MaxCartItemQuantity = 3;

        await _context.SaveChangesAsync();

        await _handler.HandleAsync(
            new AddToCartRequest(product.Id, 2));

        var result = await _handler.HandleAsync(
            new AddToCartRequest(product.Id, 2));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "MaximumCartItemQuantityExceeded");

        var cartItem = await _context.CartItems.SingleAsync();

        Assert.Equal(2, cartItem.Quantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var result = await _handler.HandleAsync(
            new AddToCartRequest(0, 0));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.NotEmpty(result.Errors);

        Assert.Empty(await _context.ShoppingCarts.ToListAsync());
        Assert.Empty(await _context.CartItems.ToListAsync());
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
