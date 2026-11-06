using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class StockAdjustmentRequest : BaseEntity<long>
{
    public int ProductId { get; set; }
    public int TargetStockQuantity { get; set; }
    public StockAdjustmentRequestStatus Status { get; set; } = StockAdjustmentRequestStatus.Pending;
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Product Product { get; set; } = null!;
}