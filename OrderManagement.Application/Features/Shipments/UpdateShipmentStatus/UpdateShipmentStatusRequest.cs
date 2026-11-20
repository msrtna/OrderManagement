using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Shipments.UpdateShipmentStatus;

public sealed record UpdateShipmentStatusRequest(
    long ShipmentId,
    ShipmentStatus Status,
    string? Description);