using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class ShoppingCart : BaseEntity<long>
{
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
