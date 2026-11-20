
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Orders.GetShipmentByOrderId;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Orders.GetShipmentByOrderId;

public sealed class GetShipmentByOrderIdHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetShipmentByOrderIdHandler _handler = null!;

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

        _handler = new GetShipmentByOrderIdHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task Handle_ShouldReturnShipmentDetails_WhenOrderBelongsToCurrentUser()
    {
        var order = await SeedOrderAsync(
            "user-1",
            includeShipment: true);

        var result = await _handler.Handle(
            new GetShipmentByOrderIdRequest(order.Id),
            CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(order.Shipment!.Id, result.ShipmentId);
        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(ShipmentStatus.Preparing, result.Status);
        Assert.NotEqual(default, result.CreatedAt);
        Assert.Null(result.ShippedAt);
        Assert.Null(result.DeliveredAt);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderBelongsToAnotherUser()
    {
        var order = await SeedOrderAsync(
            "user-2",
            includeShipment: true);

        var result = await _handler.Handle(
            new GetShipmentByOrderIdRequest(order.Id),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        var result = await _handler.Handle(
            new GetShipmentByOrderIdRequest(999),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderHasNoShipment()
    {
        var order = await SeedOrderAsync(
            "user-1",
            includeShipment: false);

        var result = await _handler.Handle(
            new GetShipmentByOrderIdRequest(order.Id),
            CancellationToken.None);

        Assert.Null(result);
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
