using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", table =>
        {
            table.HasCheckConstraint(
                "CK_CartItems_Quantity",
                "[Quantity] >= 1");
        });

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ci => ci.Quantity)
            .IsRequired();

        builder.Property(ci => ci.IsProductDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ci => ci.ShoppingCartId)
            .IsRequired();



        builder.HasOne(ci => ci.ShoppingCart)
            .WithMany(sc => sc.Items)
            .HasForeignKey(ci => ci.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ci => new { ci.ShoppingCartId, ci.ProductId })
            .IsUnique()
            .HasFilter("[ProductId] IS NOT NULL");
    }
}
