
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Orders.GetOrderStatusHistory;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Orders.GetOrderStatusHistory;

public sealed class GetOrderStatusHistoryHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetOrderStatusHistoryHandler _handler = null!;

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

        _handler = new GetOrderStatusHistoryHandler(
            _context,
            new FakeCurrentUserService("user-1"));
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderHistories_WhenOrderBelongsToCurrentUser()
    {
        var order = await SeedOrderAsync("user-1");

        await SeedHistoryAsync(
            order.Id,
            OrderStatus.PendingPayment,
            "Order created.",
            DateTime.UtcNow.AddMinutes(-10));

        await SeedHistoryAsync(
            order.Id,
            OrderStatus.Paid,
            "Payment completed.",
            DateTime.UtcNow);

        var result = await _handler.Handle(
            new GetOrderStatusHistoryRequest(order.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(2, result.Histories.Count);

        var histories = result.Histories.ToList();

        Assert.Equal(OrderStatus.PendingPayment, histories[0].Status);
        Assert.Equal("Order created.", histories[0].Description);

        Assert.Equal(OrderStatus.Paid, histories[1].Status);
        Assert.Equal("Payment completed.", histories[1].Description);
    }

    [Fact]
    public async Task Handle_ShouldReturnHistoriesOrderedByCreatedAtAscending()
    {
        var order = await SeedOrderAsync("user-1");

        var newerDate = DateTime.UtcNow;
        var olderDate = newerDate.AddMinutes(-10);

        await SeedHistoryAsync(
            order.Id,
            OrderStatus.Paid,
            "Payment completed.",
            newerDate);

        await SeedHistoryAsync(
            order.Id,
            OrderStatus.PendingPayment,
            "Order created.",
            olderDate);

        var result = await _handler.Handle(
            new GetOrderStatusHistoryRequest(order.Id),
            CancellationToken.None);

        Assert.NotNull(result);

        var histories = result.Histories.ToList();

        Assert.Equal(2, histories.Count);
        Assert.Equal(OrderStatus.PendingPayment, histories[0].Status);
        Assert.Equal(OrderStatus.Paid, histories[1].Status);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderBelongsToAnotherUser()
    {
        var order = await SeedOrderAsync("user-2");

        await SeedHistoryAsync(
            order.Id,
            OrderStatus.PendingPayment,
            "Order created.",
            DateTime.UtcNow);

        var result = await _handler.Handle(
            new GetOrderStatusHistoryRequest(order.Id),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        var result = await _handler.Handle(
            new GetOrderStatusHistoryRequest(999),
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

    private async Task<Order> SeedOrderAsync(string userId)
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

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    private async Task SeedHistoryAsync(
        long orderId,
        OrderStatus status,
        string description,
        DateTime createdAt)
    {
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = status,
            ChangedByUserId = "user-1",
            Description = description,
            CreatedAt = createdAt
        });

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
