using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }

    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<ShoppingCart> ShoppingCarts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<StoreSettings> StoreSettings { get; }
    DbSet<UserAddress> UserAddresses { get; }

    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }

    DbSet<PaymentAttempt> PaymentAttempts { get; }
    DbSet<StockReservation> StockReservations { get; }

    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentStatusHistory> ShipmentStatusHistories { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}