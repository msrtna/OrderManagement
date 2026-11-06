using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class ShipmentStatusHistory : BaseEntity<long>
{
    public long ShipmentId { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ShipmentStatus Status { get; set; }

    public Shipment Shipment { get; set; } = null!;
}