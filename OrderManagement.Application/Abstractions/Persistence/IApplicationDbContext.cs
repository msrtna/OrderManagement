
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    DbSet<Category> Categories { get; }

    DbSet<ShoppingCart> ShoppingCarts { get; }

    DbSet<CartItem> CartItems { get; }

    DbSet<StoreSettings> StoreSettings { get; }

    DbSet<UserAddress> UserAddresses { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
