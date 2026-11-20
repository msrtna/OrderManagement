using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Orders.GetOrderById;

public sealed record GetOrderByIdResponse(
    long Id,
    OrderStatus Status,
    decimal ShippingCost,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    ShippingAddressResponse ShippingAddress,
    IReadOnlyCollection<OrderItemResponse> Items,
    ShipmentResponse? Shipment);

public sealed record ShippingAddressResponse(
    string RecipientName,
    string PhoneNumber,
    string Province,
    string City,
    string AddressLine,
    string PostalCode);

public sealed record OrderItemResponse(
    long Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public sealed record ShipmentResponse(
    ShipmentStatus Status,
    DateTime? ShippedAt,
    DateTime? DeliveredAt);