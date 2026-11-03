using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
