using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class Shipment : BaseEntity<long>
{
    public long OrderId { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Preparing;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public Order Order { get; set; } = null!;
    public ICollection<ShipmentStatusHistory> StatusHistories { get; set; } = new List<ShipmentStatusHistory>();
}