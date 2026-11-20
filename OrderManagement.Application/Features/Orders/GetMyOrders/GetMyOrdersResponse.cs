using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Orders.GetMyOrders;

public sealed record GetMyOrdersResponse(
    IReadOnlyCollection<MyOrderListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record MyOrderListItemResponse(
    long Id,
    OrderStatus Status,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt,
    ShipmentStatus? ShipmentStatus);