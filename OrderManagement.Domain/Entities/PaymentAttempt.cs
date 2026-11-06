using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class PaymentAttempt : BaseEntity<long>
{
    public long OrderId { get; set; }
    public int AttemptNumber { get; set; }
    public decimal Amount { get; set; }
    public PaymentAttemptStatus Status { get; set; } = PaymentAttemptStatus.Pending;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }

    public Order Order { get; set; } = null!;
    public ICollection<StockReservation> StockReservations { get; set; } = new List<StockReservation>();
}
