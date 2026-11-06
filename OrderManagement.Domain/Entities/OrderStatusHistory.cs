using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class OrderStatusHistory : BaseEntity<long>
{
    public long OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Order Order { get; set; } = null!;
}