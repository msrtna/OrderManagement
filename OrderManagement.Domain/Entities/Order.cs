using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrderNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Status Status { get; set; }
    }
}
