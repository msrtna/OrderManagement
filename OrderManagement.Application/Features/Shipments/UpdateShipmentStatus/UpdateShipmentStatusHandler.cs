
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Shipments.UpdateShipmentStatus;

public sealed class UpdateShipmentStatusHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateShipmentStatusHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(
        UpdateShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
            return false;

        var isValidTransition = shipment.Status switch
        {
            ShipmentStatus.Preparing =>
                request.Status is ShipmentStatus.Shipped
                    or ShipmentStatus.Cancelled,

            ShipmentStatus.Shipped =>
                request.Status is ShipmentStatus.Delivered
                    or ShipmentStatus.Cancelled,

            ShipmentStatus.Delivered => false,

            ShipmentStatus.Cancelled => false,

            _ => false
        };

        if (!isValidTransition)
            throw new InvalidOperationException(
                $"Cannot change shipment status from {shipment.Status} to {request.Status}.");

        var now = DateTime.UtcNow;

        shipment.Status = request.Status;
        shipment.UpdatedAt = now;

        if (request.Status == ShipmentStatus.Shipped)
            shipment.ShippedAt = now;

        if (request.Status == ShipmentStatus.Delivered)
            shipment.DeliveredAt = now;

        var history = new Domain.Entities.ShipmentStatusHistory
        {
            ShipmentId = shipment.Id,
            Status = request.Status,
            ChangedByUserId = _currentUserService.UserId!,
            Description = request.Description,
            CreatedAt = now
        };

        await _context.ShipmentStatusHistories.AddAsync(
            history,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
