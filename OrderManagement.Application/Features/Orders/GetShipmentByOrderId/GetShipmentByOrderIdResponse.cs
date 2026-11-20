using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Orders.GetShipmentByOrderId;

public sealed record GetShipmentByOrderIdResponse(
    long ShipmentId,
    long OrderId,
    ShipmentStatus Status,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt);