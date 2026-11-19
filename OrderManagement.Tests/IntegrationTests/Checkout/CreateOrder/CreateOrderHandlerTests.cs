
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Checkout.CreateOrder;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Checkout.CreateOrder;

public sealed class CreateOrderHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private CreateOrderHandler _handler = null!;

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
        await SeedCategoryAsync();

        _handler = CreateHandler("user-1");
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateOrder_WhenRequestIsValid()
    {
        var address = await SeedAddressAsync("user-1");
        var product = await SeedProductAsync();
        await SeedCartAsync(product, quantity: 2);

        var result = await _handler.HandleAsync(
            new CreateOrderRequest(address.Id));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Empty(result.Errors);

        Assert.Equal("PendingPayment", result.Response.Status);
        Assert.Equal(2, result.Response.TotalItems);
        Assert.Equal(200m, result.Response.ItemsTotal);
        Assert.Equal(1000m, result.Response.ShippingCost);
        Assert.Equal(1200m, result.Response.TotalAmount);

        var order = await _context.Orders
            .Include(order => order.Items)
            .SingleAsync();

        Assert.Equal("user-1", order.UserId);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(1200m, order.TotalAmount);

        Assert.Equal(address.RecipientName, order.RecipientName);
        Assert.Equal(address.PhoneNumber, order.PhoneNumber);
        Assert.Equal(address.Province, order.Province);
        Assert.Equal(address.City, order.City);
        Assert.Equal(address.AddressLine, order.AddressLine);
        Assert.Equal(address.PostalCode, order.PostalCode);

        var orderItem = Assert.Single(order.Items);

        Assert.Equal(product.Id, orderItem.ProductId);
        Assert.Equal(product.Name, orderItem.ProductName);
        Assert.Equal(2, orderItem.Quantity);
        Assert.Equal(100m, orderItem.UnitPrice);
        Assert.Equal(200m, orderItem.TotalPrice);

        // Creating an order must not change stock or clear the cart.
        Assert.Equal(10, product.StockQuantity);
        Assert.Single(await _context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var address = await SeedAddressAsync("user-1");
        var product = await SeedProductAsync();
        await SeedCartAsync(product, quantity: 1);

        var handler = CreateHandler(null);

        var result = await handler.HandleAsync(
            new CreateOrderRequest(address.Id));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.Code == "UserNotAuthenticated");

        Assert.Empty(await _context.Orders.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenAddressDoesNotBelongToCurrentUser()
    {
        var address = await SeedAddressAsync("user-2");
        var product = await SeedProductAsync();
        await SeedCartAsync(product, quantity: 1);

        var result = await _handler.HandleAsync(
            new CreateOrderRequest(address.Id));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.Code == "AddressNotFound");

        Assert.Empty(await _context.Orders.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCartIsEmpty()
    {
        var address = await SeedAddressAsync("user-1");

        var result = await _handler.HandleAsync(
            new CreateOrderRequest(address.Id));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.Code == "CartNotFoundOrEmpty");

        Assert.Empty(await _context.Orders.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductIsInactive()
    {
        var address = await SeedAddressAsync("user-1");
        var product = await SeedProductAsync();

        product.IsActive = false;
        await _context.SaveChangesAsync();

        await SeedCartAsync(product, quantity: 1);

        var result = await _handler.HandleAsync(
            new CreateOrderRequest(address.Id));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.Code == "ProductNotFoundOrInactive");

        Assert.Empty(await _context.Orders.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenStockIsInsufficient()
    {
        var address = await SeedAddressAsync("user-1");
        var product = await SeedProductAsync(stockQuantity: 1);

        await SeedCartAsync(product, quantity: 2);

        var result = await _handler.HandleAsync(
            new CreateOrderRequest(address.Id));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.Code == "InsufficientStock");

        Assert.Empty(await _context.Orders.ToListAsync());
        Assert.Equal(1, product.StockQuantity);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var result = await _handler.HandleAsync(
            new CreateOrderRequest(0));

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.Code == "InvalidAddressId");

        Assert.Empty(await _context.Orders.ToListAsync());
    }

    private CreateOrderHandler CreateHandler(string? userId)
    {
        return new CreateOrderHandler(
            _context,
            new FakeCurrentUserService(userId),
            new CreateOrderRequestValidator());
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

    private async Task<UserAddress> SeedAddressAsync(string userId)
    {
        var address = new UserAddress
        {
            UserId = userId,
            Title = "Home",
            RecipientName = "Test User",
            PhoneNumber = "09123456789",
            Province = "Tehran",
            City = "Tehran",
            AddressLine = "Main Street",
            PostalCode = "1234567890",
            IsDefault = true
        };

        await _context.UserAddresses.AddAsync(address);
        await _context.SaveChangesAsync();

        return address;
    }

    private async Task<Product> SeedProductAsync(
        int stockQuantity = 10)
    {
        var product = new Product
        {
            Name = "Laptop",
            Description = "Gaming Laptop",
            Price = 100m,
            StockQuantity = stockQuantity,
            CategoryId = 1,
            IsActive = true
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return product;
    }

    private async Task SeedCartAsync(
        Product product,
        int quantity)
    {
        var cart = new ShoppingCart
        {
            UserId = "user-1"
        };

        var cartItem = new CartItem
        {
            ShoppingCart = cart,
            Product = product,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            IsProductDeleted = false
        };

        cart.Items.Add(cartItem);

        await _context.ShoppingCarts.AddAsync(cart);
        await _context.SaveChangesAsync();
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
