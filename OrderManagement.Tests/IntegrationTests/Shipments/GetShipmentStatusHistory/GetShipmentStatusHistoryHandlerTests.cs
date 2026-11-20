
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Shipments.GetShipmentStatusHistory;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Shipments.GetShipmentStatusHistory;

public sealed class GetShipmentStatusHistoryHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetShipmentStatusHistoryHandler _handler = null!;

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

        _handler = new GetShipmentStatusHistoryHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task Handle_ShouldReturnHistories_WhenShipmentBelongsToCurrentUser()
    {
        var shipment = await SeedShipmentAsync("user-1");

        var result = await _handler.Handle(
            new GetShipmentStatusHistoryRequest(shipment.Id),
            CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(shipment.Id, result.ShipmentId);
        Assert.Equal(3, result.Histories.Count);

        var histories = result.Histories.ToList();

        Assert.Equal(ShipmentStatus.Preparing, histories[0].Status);
        Assert.Equal(ShipmentStatus.Shipped, histories[1].Status);
        Assert.Equal(ShipmentStatus.Delivered, histories[2].Status);

        Assert.Equal(
            "Shipment created automatically.",
            histories[0].Description);

        Assert.Equal(
            "Shipment has been shipped.",
            histories[1].Description);

        Assert.Equal(
            "Shipment delivered successfully.",
            histories[2].Description);
    }

    [Fact]
    public async Task Handle_ShouldReturnHistoriesInChronologicalOrder()
    {
        var shipment = await SeedShipmentAsync("user-1");

        var result = await _handler.Handle(
            new GetShipmentStatusHistoryRequest(shipment.Id),
            CancellationToken.None);

        Assert.NotNull(result);

        var histories = result.Histories.ToList();

        Assert.Equal(
            histories.OrderBy(x => x.ChangedAt).ToList(),
            histories);

        Assert.True(
            histories[0].ChangedAt < histories[1].ChangedAt);

        Assert.True(
            histories[1].ChangedAt < histories[2].ChangedAt);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenShipmentBelongsToAnotherUser()
    {
        var shipment = await SeedShipmentAsync("user-2");

        var result = await _handler.Handle(
            new GetShipmentStatusHistoryRequest(shipment.Id),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenShipmentDoesNotExist()
    {
        var result = await _handler.Handle(
            new GetShipmentStatusHistoryRequest(999),
            CancellationToken.None);

        Assert.Null(result);
    }

    private async Task SeedUserAsync(string userId)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            Email = $"{userId}@test.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    private async Task<Shipment> SeedShipmentAsync(string userId)
    {
        var now = DateTime.UtcNow;

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Paid,
            ShippingCost = 1000m,
            TotalAmount = 3000m,
            ExpiresAt = now.AddDays(1),
            RecipientName = "Test Recipient",
            PhoneNumber = "09120000000",
            Province = "Tehran",
            City = "Tehran",
            AddressLine = "Test Address",
            PostalCode = "1234567890",
            CreatedAt = now
        };

        var shipment = new Shipment
        {
            Status = ShipmentStatus.Delivered,
            CreatedAt = now,
            ShippedAt = now.AddMinutes(10),
            DeliveredAt = now.AddMinutes(20)
        };

        shipment.StatusHistories.Add(new ShipmentStatusHistory
        {
            Status = ShipmentStatus.Preparing,
            ChangedByUserId = userId,
            Description = "Shipment created automatically.",
            CreatedAt = now
        });

        shipment.StatusHistories.Add(new ShipmentStatusHistory
        {
            Status = ShipmentStatus.Shipped,
            ChangedByUserId = userId,
            Description = "Shipment has been shipped.",
            CreatedAt = now.AddMinutes(10)
        });

        shipment.StatusHistories.Add(new ShipmentStatusHistory
        {
            Status = ShipmentStatus.Delivered,
            ChangedByUserId = userId,
            Description = "Shipment delivered successfully.",
            CreatedAt = now.AddMinutes(20)
        });

        order.Shipment = shipment;

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return shipment;
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
