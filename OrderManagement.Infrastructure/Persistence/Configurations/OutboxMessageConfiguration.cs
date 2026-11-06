using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", table =>
        {
            table.HasCheckConstraint(
                "CK_OutboxMessages_RetryCount",
                "[RetryCount] >= 0");

            table.HasCheckConstraint(
                "CK_OutboxMessages_ProcessingState",
                "NOT ([IsProcessed] = 1 AND [IsFailed] = 1)");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.OccurredAt)
            .IsRequired();

        builder.Property(m => m.ProcessedAt);

        builder.Property(m => m.NextAttemptAt);

        builder.Property(m => m.LockedUntil);

        builder.Property(m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.Error)
            .HasMaxLength(2000);

        builder.Property(m => m.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.IsFailed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt)
            .IsRequired();




        builder.HasIndex(m => new
        {
            m.IsProcessed,
            m.IsFailed,
            m.NextAttemptAt
        });
        builder.HasIndex(m => m.LockedUntil);
    }
}