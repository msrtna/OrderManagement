using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Orders.GetOrderStatusHistory;

public sealed record GetOrderStatusHistoryResponse(
    long OrderId,
    IReadOnlyCollection<OrderStatusHistoryItemResponse> Histories);

public sealed record OrderStatusHistoryItemResponse(
    long Id,
    OrderStatus Status,
    string? Description,
    DateTime ChangedAt);