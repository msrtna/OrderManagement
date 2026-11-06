using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint(
                "CK_Orders_ShippingCost",
                "[ShippingCost] >= 0");

            table.HasCheckConstraint(
                "CK_Orders_TotalAmount",
                "[TotalAmount] >= 0");
        });

        builder.HasKey(o => o.Id);

        // Properties
        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.ShippingCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        // Shipping Address Snapshot
        builder.Property(o => o.RecipientName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(o => o.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.Province)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.AddressLine)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.PostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.CreatedAt)
            .IsRequired();




        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.PaymentAttempts)
            .WithOne(pa => pa.Order)
            .HasForeignKey(pa => pa.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.CancellationRequests)
            .WithOne(cr => cr.Order)
            .HasForeignKey(cr => cr.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OrderStatusHistories)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => new { o.UserId, o.CreatedAt });
        builder.HasIndex(o => new { o.Status, o.ExpiresAt });
    }
}
