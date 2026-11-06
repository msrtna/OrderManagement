using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class OrderItem : BaseEntity<long>
{
    public long OrderId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
}
