
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Shipments.GetShipmentStatusHistory;

public sealed record GetShipmentStatusHistoryResponse(
    long ShipmentId,
    IReadOnlyCollection<ShipmentStatusHistoryItemResponse> Histories);

public sealed record ShipmentStatusHistoryItemResponse(
    long Id,
    ShipmentStatus Status,
    string? Description,
    DateTime ChangedAt);
