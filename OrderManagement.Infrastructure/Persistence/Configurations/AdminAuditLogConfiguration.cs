using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class AdminAuditLogConfiguration
    : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("AdminAuditLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.AdminUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(log => log.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(log => log.EntityType)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(log => log.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(log => log.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.CreatedAt)
            .IsRequired();




        builder.HasIndex(log => log.AdminUserId);
        builder.HasIndex(log => new
        {
            log.EntityType,
            log.EntityId
        });
        builder.HasIndex(log => log.CreatedAt);
    }
}
