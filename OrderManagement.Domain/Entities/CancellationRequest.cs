using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class CancellationRequest : BaseEntity<long>
{
    public long OrderId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public CancellationRequestStatus Status { get; set; } = CancellationRequestStatus.Pending;
    public string? CustomerReason { get; set; }
    public string? AdminNote { get; set; }
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser RequestedByUser { get; set; } = null!;
    public ApplicationUser? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Order Order { get; set; } = null!;
}