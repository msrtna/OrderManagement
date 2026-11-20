
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Orders.GetOrderById;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Orders.GetOrderById;

public sealed class GetOrderByIdHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetOrderByIdHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        await SeedUserAsync("user-1");
        await SeedUserAsync("user-2");

        _handler = new GetOrderByIdHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderDetails_WhenOrderBelongsToCurrentUser()
    {
        var order = await SeedOrderAsync(
            "user-1",
            includeShipment: true);

        var result = await _handler.Handle(
            new GetOrderByIdRequest(order.Id),
            CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(order.Id, result.Id);
        Assert.Equal(OrderStatus.Paid, result.Status);
        Assert.Equal(1000, result.ShippingCost);
        Assert.Equal(3000, result.TotalAmount);

        Assert.Equal("Test Recipient", result.ShippingAddress.RecipientName);
        Assert.Equal("09120000000", result.ShippingAddress.PhoneNumber);
        Assert.Equal("Tehran", result.ShippingAddress.Province);
        Assert.Equal("Tehran", result.ShippingAddress.City);
        Assert.Equal("Test Address", result.ShippingAddress.AddressLine);
        Assert.Equal("1234567890", result.ShippingAddress.PostalCode);

        Assert.Single(result.Items);

        var item = result.Items.First();

        Assert.Equal("Test Product", item.ProductName);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(1000, item.UnitPrice);
        Assert.Equal(2000, item.TotalPrice);

        Assert.NotNull(result.Shipment);
        Assert.Equal(ShipmentStatus.Preparing, result.Shipment.Status);
        Assert.Null(result.Shipment.ShippedAt);
        Assert.Null(result.Shipment.DeliveredAt);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderBelongsToAnotherUser()
    {
        var order = await SeedOrderAsync("user-2");

        var result = await _handler.Handle(
            new GetOrderByIdRequest(order.Id),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        var result = await _handler.Handle(
            new GetOrderByIdRequest(999),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNullShipment_WhenOrderHasNoShipment()
    {
        var order = await SeedOrderAsync("user-1");

        var result = await _handler.Handle(
            new GetOrderByIdRequest(order.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Shipment);
    }

    private async Task SeedUserAsync(string userId)
    {
        _context.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            Email = $"{userId}@test.com",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private async Task<Order> SeedOrderAsync(
        string userId,
        bool includeShipment = false)
    {
        var now = DateTime.UtcNow;

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Paid,
            ShippingCost = 1000,
            TotalAmount = 3000,
            ExpiresAt = now.AddDays(1),
            RecipientName = "Test Recipient",
            PhoneNumber = "09120000000",
            Province = "Tehran",
            City = "Tehran",
            AddressLine = "Test Address",
            PostalCode = "1234567890",
            CreatedAt = now
        };

        order.Items.Add(new OrderItem
        {
            ProductName = "Test Product",
            Quantity = 2,
            UnitPrice = 1000,
            TotalPrice = 2000,
            CreatedAt = now
        });

        if (includeShipment)
        {
            order.Shipment = new Shipment
            {
                Status = ShipmentStatus.Preparing,
                CreatedAt = now
            };
        }

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        return order;
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
