
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Shipments.GetShipmentStatusHistory;

public sealed class GetShipmentStatusHistoryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetShipmentStatusHistoryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetShipmentStatusHistoryResponse?> Handle(
        GetShipmentStatusHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var shipmentExists = await _context.Shipments
            .AsNoTracking()
            .AnyAsync(
                shipment =>
                    shipment.Id == request.ShipmentId &&
                    shipment.Order.UserId == userId,
                cancellationToken);

        if (!shipmentExists)
            return null;

        var histories = await _context.ShipmentStatusHistories
            .AsNoTracking()
            .Where(history => history.ShipmentId == request.ShipmentId)
            .OrderBy(history => history.CreatedAt)
            .ThenBy(history => history.Id)
            .Select(history => new ShipmentStatusHistoryItemResponse(
                history.Id,
                history.Status,
                history.Description,
                history.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetShipmentStatusHistoryResponse(
            request.ShipmentId,
            histories);
    }
}
