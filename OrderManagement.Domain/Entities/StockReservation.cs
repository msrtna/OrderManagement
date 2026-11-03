using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities
{
    public class StockReservation
    {
        public long Id { get; set; }
        public long PaymentAttemptId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public StockReservationStatus Status { get; set; }
    }
}
