using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderId)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();




        builder.HasOne(s => s.Order)
            .WithOne(o => o.Shipment)
            .HasForeignKey<Shipment>(s => s.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.StatusHistories)
            .WithOne(sh => sh.Shipment)
            .HasForeignKey(sh => sh.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasIndex(s => s.OrderId)
            .IsUnique();
    }
}
