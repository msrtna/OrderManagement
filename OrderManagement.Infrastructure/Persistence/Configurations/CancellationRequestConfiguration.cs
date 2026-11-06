
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class CancellationRequestConfiguration
    : IEntityTypeConfiguration<CancellationRequest>
{
    public void Configure(EntityTypeBuilder<CancellationRequest> builder)
    {
        builder.ToTable("CancellationRequests");

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.OrderId)
            .IsRequired();

        builder.Property(cr => cr.RequestedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(cr => cr.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(cr => cr.CustomerReason)
            .HasMaxLength(1000);

        builder.Property(cr => cr.AdminNote)
            .HasMaxLength(1000);

        builder.Property(cr => cr.ReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(cr => cr.ReviewedAt);

        builder.Property(cr => cr.CreatedAt)
            .IsRequired();




        builder.HasOne(cr => cr.RequestedByUser)
            .WithMany()
            .HasForeignKey(cr => cr.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cr => cr.ReviewedByUser)
            .WithMany()
            .HasForeignKey(cr => cr.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasIndex(cr => cr.OrderId);
        builder.HasIndex(cr => new { cr.Status, cr.CreatedAt });
        builder.HasIndex(cr => cr.RequestedByUserId);
        builder.HasIndex(cr => cr.ReviewedByUserId);
    }
}
