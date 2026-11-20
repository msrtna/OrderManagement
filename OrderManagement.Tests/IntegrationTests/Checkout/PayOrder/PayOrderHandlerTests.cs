
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Checkout.PayOrder;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Checkout.PayOrder;

public sealed class PayOrderHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private PayOrderHandler _handler = null!;

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
    public async Task HandleAsync_ShouldCompletePayment_WhenRequestIsValid()
    {
        var product = await SeedProductAsync();
        var order = await SeedOrderAsync(product, 2, 100m);

        await SeedCartAsync(product, 2);

        product.Price = 500m;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Empty(result.Errors);

        Assert.Equal(order.Id, result.Response.OrderId);
        Assert.Equal("Paid", result.Response.OrderStatus);
        Assert.Equal("Succeeded", result.Response.PaymentStatus);
        Assert.Equal(1200m, result.Response.Amount);
        Assert.True(result.Response.PaymentAttemptId > 0);

        var savedOrder = await _context.Orders
            .AsNoTracking()
            .Include(savedOrder => savedOrder.Items)
            .SingleAsync(savedOrder => savedOrder.Id == order.Id);

        Assert.Equal(OrderStatus.Paid, savedOrder.Status);
        Assert.Equal(1200m, savedOrder.TotalAmount);

        var savedOrderItem = Assert.Single(savedOrder.Items);

        Assert.Equal(100m, savedOrderItem.UnitPrice);
        Assert.Equal(200m, savedOrderItem.TotalPrice);
        Assert.Equal(2, savedOrderItem.Quantity);

        var savedProduct = await _context.Products
            .AsNoTracking()
            .SingleAsync(savedProduct => savedProduct.Id == product.Id);

        Assert.Equal(8, savedProduct.StockQuantity);
        Assert.Equal(500m, savedProduct.Price);

        var paymentAttempt = await _context.PaymentAttempts
            .AsNoTracking()
            .Include(attempt => attempt.StockReservations)
            .SingleAsync(attempt => attempt.OrderId == order.Id);

        Assert.Equal(PaymentAttemptStatus.Succeeded, paymentAttempt.Status);
        Assert.Equal(1, paymentAttempt.AttemptNumber);
        Assert.Equal(1200m, paymentAttempt.Amount);
        Assert.NotNull(paymentAttempt.CompletedAt);

        var reservation = Assert.Single(
            paymentAttempt.StockReservations);

        Assert.Equal(product.Id, reservation.ProductId);
        Assert.Equal(2, reservation.Quantity);
        Assert.Equal(StockReservationStatus.Consumed, reservation.Status);
        Assert.NotNull(reservation.CompletedAt);

        Assert.Empty(await _context.CartItems.ToListAsync());

        var history = await _context.OrderStatusHistories
            .AsNoTracking()
            .SingleAsync(history => history.OrderId == order.Id);

        Assert.Equal(OrderStatus.Paid, history.Status);
        Assert.Equal("user-1", history.ChangedByUserId);
        Assert.Equal(
            "Payment completed successfully.",
            history.Description);

        // Assert shipment
        var shipment = await _context.Shipments
            .AsNoTracking()
            .Include(shipment => shipment.StatusHistories)
            .SingleOrDefaultAsync(
                shipment => shipment.OrderId == order.Id);

        Assert.NotNull(shipment);

        Assert.Equal(
            ShipmentStatus.Preparing,
            shipment.Status);

        Assert.Null(shipment.ShippedAt);
        Assert.Null(shipment.DeliveredAt);

        Assert.Single(shipment.StatusHistories);

        var shipmentHistory = shipment.StatusHistories.Single();

        Assert.Equal(
            ShipmentStatus.Preparing,
            shipmentHistory.Status);

        Assert.Equal(
            "user-1",
            shipmentHistory.ChangedByUserId);

        Assert.Equal(
            "Shipment created automatically after successful payment.",
            shipmentHistory.Description);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        var product = await SeedProductAsync();
        var order = await SeedOrderAsync(product, 1, 100m);

        var handler = CreateHandler(null);

        var result = await handler.HandleAsync(
            new PayOrderRequest(order.Id));

        AssertFailure(result, "UserNotAuthenticated");

        await AssertNoPaymentChangesAsync(order.Id, product.Id, 10);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenOrderDoesNotExist()
    {
        var result = await _handler.HandleAsync(
            new PayOrderRequest(999));

        AssertFailure(result, "OrderNotFound");

        Assert.Empty(await _context.PaymentAttempts.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenOrderBelongsToAnotherUser()
    {
        var product = await SeedProductAsync();
        var order = await SeedOrderAsync(
            product,
            1,
            100m,
            userId: "user-2");

        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        AssertFailure(result, "OrderNotFound");

        await AssertNoPaymentChangesAsync(order.Id, product.Id, 10);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenOrderIsAlreadyPaid()
    {
        var product = await SeedProductAsync();
        var order = await SeedOrderAsync(product, 1, 100m);

        order.Status = OrderStatus.Paid;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        AssertFailure(result, "OrderNotPayable");

        await AssertNoPaymentChangesAsync(
            order.Id,
            product.Id,
            10,
            OrderStatus.Paid);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenOrderHasExpired()
    {
        var product = await SeedProductAsync();
        var order = await SeedOrderAsync(product, 1, 100m);

        order.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        AssertFailure(result, "OrderExpired");

        await AssertNoPaymentChangesAsync(order.Id, product.Id, 10);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductIsInactive()
    {
        var product = await SeedProductAsync();
        var order = await SeedOrderAsync(product, 1, 100m);

        product.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        AssertFailure(
            result,
            "InsufficientStockOrInactiveProduct");

        await AssertNoPaymentChangesAsync(order.Id, product.Id, 10);

        Assert.Empty(await _context.Shipments
    .Where(shipment => shipment.OrderId == order.Id)
    .ToListAsync());

        Assert.Empty(await _context.ShipmentStatusHistories
            .ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenStockIsInsufficient()
    {
        var product = await SeedProductAsync(stockQuantity: 1);
        var order = await SeedOrderAsync(product, 2, 100m);

        await SeedCartAsync(product, 2);

        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        AssertFailure(
            result,
            "InsufficientStockOrInactiveProduct");

        await AssertNoPaymentChangesAsync(order.Id, product.Id, 1);

        Assert.Single(await _context.CartItems.ToListAsync());

        Assert.Empty(await _context.Shipments
    .Where(shipment => shipment.OrderId == order.Id)
    .ToListAsync());

        Assert.Empty(await _context.ShipmentStatusHistories
            .ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var result = await _handler.HandleAsync(
            new PayOrderRequest(0));

        AssertFailure(result, "InvalidOrderId");

        Assert.Empty(await _context.PaymentAttempts.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenPaymentAttemptIsAlreadyPending()
    {
        var product = await SeedProductAsync();
        var order = await SeedOrderAsync(product, 1, 100m);

        var attempt = new PaymentAttempt
        {
            OrderId = order.Id,
            AttemptNumber = 1,
            Amount = order.TotalAmount,
            Status = PaymentAttemptStatus.Pending,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _context.PaymentAttempts.AddAsync(attempt);
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        AssertFailure(result, "PaymentAlreadyInProgress");

        Assert.Single(await _context.PaymentAttempts.ToListAsync());

        var savedProduct = await _context.Products
            .AsNoTracking()
            .SingleAsync(savedProduct => savedProduct.Id == product.Id);

        Assert.Equal(10, savedProduct.StockQuantity);
    }


    [Fact]
    public async Task HandleAsync_ShouldRollbackAllStockChanges_WhenOneProductHasInsufficientStock()
    {
        // Arrange
        var firstProduct = await SeedProductAsync(stockQuantity: 10);
        var secondProduct = await SeedProductAsync(stockQuantity: 1);

        var order = new Order
        {
            UserId = "user-1",
            Status = OrderStatus.PendingPayment,
            ShippingCost = 1000m,
            TotalAmount = 1300m,
            ExpiresAt = DateTime.UtcNow.AddHours(24),

            RecipientName = "Test User",
            PhoneNumber = "09123456789",
            Province = "Tehran",
            City = "Tehran",
            AddressLine = "Main Street",
            PostalCode = "1234567890",
            CreatedAt = DateTime.UtcNow
        };

        order.Items.Add(new OrderItem
        {
            Product = firstProduct,
            ProductId = firstProduct.Id,
            ProductName = firstProduct.Name,
            Quantity = 2,
            UnitPrice = 100m,
            TotalPrice = 200m,
            CreatedAt = DateTime.UtcNow
        });

        order.Items.Add(new OrderItem
        {
            Product = secondProduct,
            ProductId = secondProduct.Id,
            ProductName = secondProduct.Name,
            Quantity = 2,
            UnitPrice = 50m,
            TotalPrice = 100m,
            CreatedAt = DateTime.UtcNow
        });

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(
            new PayOrderRequest(order.Id));

        // Assert
        AssertFailure(
            result,
            "InsufficientStockOrInactiveProduct");

        var savedFirstProduct = await _context.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == firstProduct.Id);

        var savedSecondProduct = await _context.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == secondProduct.Id);

        // Both products must retain their original stock.
        Assert.Equal(10, savedFirstProduct.StockQuantity);
        Assert.Equal(1, savedSecondProduct.StockQuantity);

        var savedOrder = await _context.Orders
            .AsNoTracking()
            .SingleAsync(savedOrder => savedOrder.Id == order.Id);

        Assert.Equal(
            OrderStatus.PendingPayment,
            savedOrder.Status);

        Assert.Empty(await _context.PaymentAttempts.ToListAsync());
        Assert.Empty(await _context.StockReservations.ToListAsync());
        Assert.Empty(await _context.OrderStatusHistories.ToListAsync());
    }


    private PayOrderHandler CreateHandler(string? userId)
    {
        return new PayOrderHandler(
            _context,
            new FakeCurrentUserService(userId),
            new PayOrderRequestValidator());
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

    private async Task<Order> SeedOrderAsync(
        Product product,
        int quantity,
        decimal unitPrice,
        string userId = "user-1")
    {
        var now = DateTime.UtcNow;

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            ShippingCost = 1000m,
            TotalAmount = quantity * unitPrice + 1000m,
            ExpiresAt = now.AddHours(24),

            RecipientName = "Test User",
            PhoneNumber = "09123456789",
            Province = "Tehran",
            City = "Tehran",
            AddressLine = "Main Street",
            PostalCode = "1234567890",

            CreatedAt = now
        };

        order.Items.Add(new OrderItem
        {
            Product = product,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            CreatedAt = now
        });

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return order;
    }

    private async Task SeedCartAsync(
        Product product,
        int quantity)
    {
        var cart = new ShoppingCart
        {
            UserId = "user-1"
        };

        cart.Items.Add(new CartItem
        {
            ShoppingCart = cart,
            Product = product,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            IsProductDeleted = false
        });

        await _context.ShoppingCarts.AddAsync(cart);
        await _context.SaveChangesAsync();
    }

    private async Task AssertNoPaymentChangesAsync(
        long orderId,
        int productId,
        int expectedStock,
        OrderStatus expectedOrderStatus = OrderStatus.PendingPayment)
    {
        var savedOrder = await _context.Orders
            .AsNoTracking()
            .SingleAsync(order => order.Id == orderId);

        Assert.Equal(expectedOrderStatus, savedOrder.Status);

        var savedProduct = await _context.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == productId);

        Assert.Equal(expectedStock, savedProduct.StockQuantity);

        Assert.Empty(await _context.PaymentAttempts
            .Where(attempt => attempt.OrderId == orderId)
            .ToListAsync());

        Assert.Empty(await _context.StockReservations.ToListAsync());

        Assert.Empty(await _context.OrderStatusHistories
            .Where(history => history.OrderId == orderId)
            .ToListAsync());
    }

    private static void AssertFailure(
        PayOrderResult result,
        string expectedErrorCode)
    {
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.Code == expectedErrorCode);
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
