using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class CartItem : BaseEntity<long>
{
    public long ShoppingCartId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsProductDeleted { get; set; } = false;

    public ShoppingCart ShoppingCart { get; set; } = null!;
    public Product? Product { get; set; }
}
