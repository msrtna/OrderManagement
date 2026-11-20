
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Shipments.UpdateShipmentStatus;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Shipments.UpdateShipmentStatus;

public sealed class UpdateShipmentStatusHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private UpdateShipmentStatusHandler _handler = null!;

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

        _handler = CreateHandler("user-1");
    }

    [Fact]
    public async Task Handle_ShouldUpdateStatusToShipped_WhenShipmentIsPreparing()
    {
        var shipment = await SeedShipmentAsync(
            ShipmentStatus.Preparing);

        var result = await _handler.Handle(
            new UpdateShipmentStatusRequest(
                shipment.Id,
                ShipmentStatus.Shipped,
                "Shipment has been shipped."),
            CancellationToken.None);

        Assert.True(result);

        var savedShipment = await _context.Shipments
            .AsNoTracking()
            .SingleAsync(x => x.Id == shipment.Id);

        Assert.Equal(ShipmentStatus.Shipped, savedShipment.Status);
        Assert.NotNull(savedShipment.ShippedAt);
        Assert.Null(savedShipment.DeliveredAt);
        Assert.NotNull(savedShipment.UpdatedAt);

        var histories = await GetHistoriesAsync(shipment.Id);

        Assert.Equal(2, histories.Count);

        var history = histories.Last();

        Assert.Equal(ShipmentStatus.Shipped, history.Status);
        Assert.Equal("user-1", history.ChangedByUserId);
        Assert.Equal("Shipment has been shipped.", history.Description);
    }

    [Fact]
    public async Task Handle_ShouldUpdateStatusToCancelled_WhenShipmentIsPreparing()
    {
        var shipment = await SeedShipmentAsync(
            ShipmentStatus.Preparing);

        var result = await _handler.Handle(
            new UpdateShipmentStatusRequest(
                shipment.Id,
                ShipmentStatus.Cancelled,
                "Shipment cancelled."),
            CancellationToken.None);

        Assert.True(result);

        var savedShipment = await _context.Shipments
            .AsNoTracking()
            .SingleAsync(x => x.Id == shipment.Id);

        Assert.Equal(ShipmentStatus.Cancelled, savedShipment.Status);
        Assert.Null(savedShipment.ShippedAt);
        Assert.Null(savedShipment.DeliveredAt);

        var histories = await GetHistoriesAsync(shipment.Id);

        Assert.Equal(2, histories.Count);

        var history = histories.Last();

        Assert.Equal(ShipmentStatus.Cancelled, history.Status);
        Assert.Equal("user-1", history.ChangedByUserId);
        Assert.Equal("Shipment cancelled.", history.Description);
    }

    [Fact]
    public async Task Handle_ShouldUpdateStatusToDelivered_WhenShipmentIsShipped()
    {
        var shipment = await SeedShipmentAsync(
            ShipmentStatus.Shipped);

        var result = await _handler.Handle(
            new UpdateShipmentStatusRequest(
                shipment.Id,
                ShipmentStatus.Delivered,
                "Shipment delivered successfully."),
            CancellationToken.None);

        Assert.True(result);

        var savedShipment = await _context.Shipments
            .AsNoTracking()
            .SingleAsync(x => x.Id == shipment.Id);

        Assert.Equal(ShipmentStatus.Delivered, savedShipment.Status);
        Assert.NotNull(savedShipment.ShippedAt);
        Assert.NotNull(savedShipment.DeliveredAt);

        var histories = await GetHistoriesAsync(shipment.Id);

        Assert.Equal(2, histories.Count);

        var history = histories.Last();

        Assert.Equal(ShipmentStatus.Delivered, history.Status);
        Assert.Equal("user-1", history.ChangedByUserId);
        Assert.Equal(
            "Shipment delivered successfully.",
            history.Description);
    }

    [Fact]
    public async Task Handle_ShouldUpdateStatusToCancelled_WhenShipmentIsShipped()
    {
        var shipment = await SeedShipmentAsync(
            ShipmentStatus.Shipped);

        var result = await _handler.Handle(
            new UpdateShipmentStatusRequest(
                shipment.Id,
                ShipmentStatus.Cancelled,
                "Shipment cancelled."),
            CancellationToken.None);

        Assert.True(result);

        var savedShipment = await _context.Shipments
            .AsNoTracking()
            .SingleAsync(x => x.Id == shipment.Id);

        Assert.Equal(ShipmentStatus.Cancelled, savedShipment.Status);
        Assert.NotNull(savedShipment.ShippedAt);
        Assert.Null(savedShipment.DeliveredAt);

        var histories = await GetHistoriesAsync(shipment.Id);

        Assert.Equal(2, histories.Count);

        Assert.Equal(
            ShipmentStatus.Cancelled,
            histories.Last().Status);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenTransitionIsInvalid()
    {
        var shipment = await SeedShipmentAsync(
            ShipmentStatus.Preparing);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new UpdateShipmentStatusRequest(
                    shipment.Id,
                    ShipmentStatus.Delivered,
                    "Invalid transition."),
                CancellationToken.None));

        var savedShipment = await _context.Shipments
            .AsNoTracking()
            .SingleAsync(x => x.Id == shipment.Id);

        Assert.Equal(
            ShipmentStatus.Preparing,
            savedShipment.Status);

        var histories = await GetHistoriesAsync(shipment.Id);

        Assert.Single(histories);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenShipmentIsAlreadyDelivered()
    {
        var shipment = await SeedShipmentAsync(
            ShipmentStatus.Delivered);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new UpdateShipmentStatusRequest(
                    shipment.Id,
                    ShipmentStatus.Cancelled,
                    "Invalid transition."),
                CancellationToken.None));

        var savedShipment = await _context.Shipments
            .AsNoTracking()
            .SingleAsync(x => x.Id == shipment.Id);

        Assert.Equal(
            ShipmentStatus.Delivered,
            savedShipment.Status);

        var histories = await GetHistoriesAsync(shipment.Id);

        Assert.Single(histories);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenShipmentDoesNotExist()
    {
        var result = await _handler.Handle(
            new UpdateShipmentStatusRequest(
                999,
                ShipmentStatus.Shipped,
                "Shipment shipped."),
            CancellationToken.None);

        Assert.False(result);

        Assert.Empty(await _context.ShipmentStatusHistories
            .ToListAsync());
    }

    private UpdateShipmentStatusHandler CreateHandler(string? userId)
    {
        return new UpdateShipmentStatusHandler(
            _context,
            new FakeCurrentUserService(userId));
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

    private async Task<Shipment> SeedShipmentAsync(
        ShipmentStatus status)
    {
        var now = DateTime.UtcNow;

        var order = new Order
        {
            UserId = "user-1",
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
            Status = status,
            CreatedAt = now,
            ShippedAt = status is ShipmentStatus.Shipped
                or ShipmentStatus.Delivered
                    ? now.AddMinutes(-10)
                    : null,
            DeliveredAt = status == ShipmentStatus.Delivered
                ? now.AddMinutes(-5)
                : null
        };

        order.Shipment = shipment;

        shipment.StatusHistories.Add(new ShipmentStatusHistory
        {
            Status = status,
            ChangedByUserId = "user-1",
            Description = "Initial shipment status.",
            CreatedAt = now
        });

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return shipment;
    }

    private async Task<List<ShipmentStatusHistory>> GetHistoriesAsync(
        long shipmentId)
    {
        return await _context.ShipmentStatusHistories
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();
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
