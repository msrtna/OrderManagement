using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities
{
    public class PaymentAttempt
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public int AttemptNumber { get; set; }
        public decimal Amount { get; set; }
        public PaymentAttemptStatus status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ProviderReference { get; set; }
        public string? FailureReason { get; set; }
    }
}
