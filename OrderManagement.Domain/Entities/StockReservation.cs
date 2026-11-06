using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class StockReservation : BaseEntity<long>
{
    public long PaymentAttemptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public StockReservationStatus Status { get; set; } = StockReservationStatus.Active;

    public PaymentAttempt PaymentAttempt { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
