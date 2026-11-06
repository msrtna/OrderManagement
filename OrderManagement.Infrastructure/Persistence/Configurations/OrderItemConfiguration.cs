
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderItems_Quantity",
                "[Quantity] >= 1");

            table.HasCheckConstraint(
                "CK_OrderItems_UnitPrice",
                "[UnitPrice] >= 0");

            table.HasCheckConstraint(
                "CK_OrderItems_TotalPrice",
                "[TotalPrice] >= 0");

            table.HasCheckConstraint(
                "CK_OrderItems_TotalPriceCalculation",
                "[TotalPrice] = [Quantity] * [UnitPrice]");
        });

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.OrderId)
            .IsRequired();

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        builder.Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(oi => oi.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(oi => oi.CreatedAt)
            .IsRequired();
    }
}
