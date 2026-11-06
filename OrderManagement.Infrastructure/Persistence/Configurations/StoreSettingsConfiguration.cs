using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("StoreSettings", table =>
        {
            table.HasCheckConstraint(
                "CK_StoreSettings_SingleRow",
                "[Id] = 1");

            table.HasCheckConstraint(
                "CK_StoreSettings_ReservationDurationMinutes",
                "[ReservationDurationMinutes] > 0");

            table.HasCheckConstraint(
                "CK_StoreSettings_OrderExpirationHours",
                "[OrderExpirationHours] > 0");

            table.HasCheckConstraint(
                "CK_StoreSettings_DefaultShippingCost",
                "[DefaultShippingCost] >= 0");

            table.HasCheckConstraint(
                "CK_StoreSettings_MaxCartItemQuantity",
                "[MaxCartItemQuantity] > 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ReservationDurationMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(s => s.OrderExpirationHours)
            .IsRequired()
            .HasDefaultValue(24);

        builder.Property(s => s.DefaultShippingCost)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(1000m);

        builder.Property(s => s.MaxCartItemQuantity)
            .IsRequired()
            .HasDefaultValue(99);

        builder.Property(s => s.CreatedAt)
            .IsRequired();
    }
}
