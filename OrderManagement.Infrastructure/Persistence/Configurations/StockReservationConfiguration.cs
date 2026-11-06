using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations", table =>
        {
            table.HasCheckConstraint(
                "CK_StockReservations_Quantity",
                "[Quantity] >= 1");
        });

        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.PaymentAttemptId)
            .IsRequired();

        builder.Property(sr => sr.ProductId)
            .IsRequired();

        builder.Property(sr => sr.Quantity)
            .IsRequired();

        builder.Property(sr => sr.ExpiresAt)
            .IsRequired();

        builder.Property(sr => sr.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(sr => sr.CreatedAt)
            .IsRequired();




        builder.HasIndex(sr => new { sr.PaymentAttemptId, sr.ProductId })
            .IsUnique();

        builder.HasIndex(sr => new { sr.Status, sr.ExpiresAt });
    }
}
