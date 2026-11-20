
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Orders.GetShipmentByOrderId;

public sealed class GetShipmentByOrderIdHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetShipmentByOrderIdHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetShipmentByOrderIdResponse?> Handle(
        GetShipmentByOrderIdRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        return await _context.Shipments
            .AsNoTracking()
            .Where(shipment =>
                shipment.OrderId == request.OrderId &&
                shipment.Order.UserId == userId)
            .Select(shipment => new GetShipmentByOrderIdResponse(
                shipment.Id,
                shipment.OrderId,
                shipment.Status,
                shipment.CreatedAt,
                shipment.ShippedAt,
                shipment.DeliveredAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
