using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts", table =>
        {
            table.HasCheckConstraint(
                "CK_PaymentAttempts_AttemptNumber",
                "[AttemptNumber] >= 1");

            table.HasCheckConstraint(
                "CK_PaymentAttempts_Amount",
                "[Amount] >= 0");
        });

        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.AttemptNumber)
            .IsRequired();

        builder.Property(pa => pa.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pa => pa.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pa => pa.StartedAt)
            .IsRequired();

        builder.Property(pa => pa.ProviderReference)
            .HasMaxLength(200);

        builder.Property(pa => pa.FailureReason)
            .HasMaxLength(1000);

        builder.Property(pa => pa.CreatedAt)
            .IsRequired();




        builder.HasMany(pa => pa.StockReservations)
            .WithOne(sr => sr.PaymentAttempt)
            .HasForeignKey(sr => sr.PaymentAttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pa => new { pa.OrderId, pa.AttemptNumber })
            .IsUnique();

        builder.HasIndex(pa => pa.OrderId)
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'Unknown')");


        builder.HasIndex(pa => pa.ProviderReference);
    }
}
