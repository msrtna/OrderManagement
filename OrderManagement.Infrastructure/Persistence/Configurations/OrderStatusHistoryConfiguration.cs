using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderStatusHistoryConfiguration
    : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.OrderId)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(h => h.Description)
            .HasMaxLength(1000);

        builder.Property(h => h.CreatedAt)
            .IsRequired();



        builder.HasIndex(h => new { h.OrderId, h.CreatedAt });
    }
}
