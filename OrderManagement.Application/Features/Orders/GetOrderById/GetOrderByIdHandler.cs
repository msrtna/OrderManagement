
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Orders.GetOrderById;

public sealed class GetOrderByIdHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderByIdHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetOrderByIdResponse?> Handle(
        GetOrderByIdRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        return await _context.Orders
            .AsNoTracking()
            .Where(order =>
                order.Id == request.OrderId &&
                order.UserId == userId)
            .Select(order => new GetOrderByIdResponse(
                order.Id,
                order.Status,
                order.ShippingCost,
                order.TotalAmount,
                order.CreatedAt,
                order.ExpiresAt,
                new ShippingAddressResponse(
                    order.RecipientName,
                    order.PhoneNumber,
                    order.Province,
                    order.City,
                    order.AddressLine,
                    order.PostalCode),
                order.Items
                    .Select(item => new OrderItemResponse(
                        item.Id,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.TotalPrice))
                    .ToList(),
                order.Shipment == null
                    ? null
                    : new ShipmentResponse(
                        order.Shipment.Status,
                        order.Shipment.ShippedAt,
                        order.Shipment.DeliveredAt)))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
