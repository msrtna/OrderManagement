using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class ShipmentStatusHistoryConfiguration
    : IEntityTypeConfiguration<ShipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
    {
        builder.ToTable("ShipmentStatusHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ShipmentId)
            .IsRequired();

        builder.Property(h => h.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(h => h.Description)
            .HasMaxLength(1000);

        builder.Property(h => h.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .IsRequired();



        builder.HasIndex(h => new { h.ShipmentId, h.CreatedAt });
    }
}
