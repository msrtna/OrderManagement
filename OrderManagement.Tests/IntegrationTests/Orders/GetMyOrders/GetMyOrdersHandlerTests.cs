
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Orders.GetMyOrders;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Orders.GetMyOrders;

public sealed class GetMyOrdersHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetMyOrdersHandler _handler = null!;

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

        _handler = new GetMyOrdersHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyCurrentUserOrders()
    {
        await SeedOrderAsync(
            "user-1",
            DateTime.UtcNow.AddDays(-2));

        await SeedOrderAsync(
            "user-2",
            DateTime.UtcNow.AddDays(-1));

        await SeedOrderAsync(
            "user-1",
            DateTime.UtcNow);

        var result = await _handler.Handle(
            new GetMyOrdersRequest(),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item =>
            Assert.DoesNotContain(
                _context.Orders.Where(o => o.UserId == "user-2")
                    .Select(o => o.Id),
                id => id == item.Id));
    }

    [Fact]
    public async Task Handle_ShouldReturnOrdersOrderedByNewestFirst()
    {
        var olderOrder = await SeedOrderAsync(
            "user-1",
            DateTime.UtcNow.AddDays(-2));

        var newerOrder = await SeedOrderAsync(
            "user-1",
            DateTime.UtcNow);

        var result = await _handler.Handle(
            new GetMyOrdersRequest(),
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(newerOrder.Id, result.Items.First().Id);
        Assert.Equal(olderOrder.Id, result.Items.Last().Id);
    }

    [Fact]
    public async Task Handle_ShouldApplyPagination()
    {
        await SeedOrderAsync("user-1", DateTime.UtcNow.AddDays(-3));
        await SeedOrderAsync("user-1", DateTime.UtcNow.AddDays(-2));
        await SeedOrderAsync("user-1", DateTime.UtcNow.AddDays(-1));
        await SeedOrderAsync("user-1", DateTime.UtcNow);

        var firstPage = await _handler.Handle(
            new GetMyOrdersRequest(PageNumber: 1, PageSize: 2),
            CancellationToken.None);

        var secondPage = await _handler.Handle(
            new GetMyOrdersRequest(PageNumber: 2, PageSize: 2),
            CancellationToken.None);

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);

        Assert.NotEqual(
    firstPage.Items.First().Id,
    secondPage.Items.First().Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnItemCountAndShipmentStatus()
    {
        var orderWithShipment = await SeedOrderAsync(
            "user-1",
            DateTime.UtcNow,
            itemCount: 2,
            shipmentStatus: ShipmentStatus.Preparing);

        var orderWithoutShipment = await SeedOrderAsync(
            "user-1",
            DateTime.UtcNow.AddDays(-1),
            itemCount: 1);

        var result = await _handler.Handle(
            new GetMyOrdersRequest(),
            CancellationToken.None);

        var firstOrder = result.Items.Single(
            item => item.Id == orderWithShipment.Id);

        var secondOrder = result.Items.Single(
            item => item.Id == orderWithoutShipment.Id);

        Assert.Equal(2, firstOrder.ItemCount);
        Assert.Equal(ShipmentStatus.Preparing, firstOrder.ShipmentStatus);

        Assert.Equal(1, secondOrder.ItemCount);
        Assert.Null(secondOrder.ShipmentStatus);
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
        DateTime createdAt,
        int itemCount = 1,
        ShipmentStatus? shipmentStatus = null)
    {
        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            ShippingCost = 1000,
            TotalAmount = 2000,
            ExpiresAt = createdAt.AddDays(1),
            RecipientName = "Test Recipient",
            PhoneNumber = "09120000000",
            Province = "Tehran",
            City = "Tehran",
            AddressLine = "Test Address",
            PostalCode = "1234567890",
            CreatedAt = createdAt
        };

        for (var i = 0; i < itemCount; i++)
        {
            order.Items.Add(new OrderItem
            {
                ProductName = $"Product {i + 1}",
                Quantity = 1,
                UnitPrice = 1000,
                TotalPrice = 1000,
                CreatedAt = createdAt
            });
        }

        if (shipmentStatus.HasValue)
        {
            order.Shipment = new Shipment
            {
                Status = shipmentStatus.Value,
                CreatedAt = createdAt
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
