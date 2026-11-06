using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class StockAdjustmentRequestConfiguration
    : IEntityTypeConfiguration<StockAdjustmentRequest>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentRequest> builder)
    {
        builder.ToTable("StockAdjustmentRequests", table =>
        {
            table.HasCheckConstraint(
                "CK_StockAdjustmentRequests_TargetStockQuantity",
                "[TargetStockQuantity] >= 0");
        });

        builder.HasKey(sar => sar.Id);

        builder.Property(sar => sar.ProductId)
            .IsRequired();

        builder.Property(sar => sar.TargetStockQuantity)
            .IsRequired();

        builder.Property(sar => sar.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(sar => sar.RequestedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(sar => sar.CompletedAt);

        builder.Property(sar => sar.CancelledAt);

        builder.Property(sar => sar.CreatedAt)
            .IsRequired();




        builder.HasIndex(sar => sar.ProductId)
            .IsUnique()
            .HasFilter("[Status] = 'Pending'");

        builder.HasIndex(sar => new { sar.Status, sar.CreatedAt });
    }
}