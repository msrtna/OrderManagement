using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Orders.GetMyOrders;

public sealed class GetMyOrdersHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyOrdersHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetMyOrdersResponse> Handle(
        GetMyOrdersRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var query = _context.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)request.PageSize);

        var items = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(order => new MyOrderListItemResponse(
                order.Id,
                order.Status,
                order.TotalAmount,
                order.Items.Count,
                order.CreatedAt,
                order.Shipment == null
                    ? null
                    : order.Shipment.Status))
            .ToListAsync(cancellationToken);

        return new GetMyOrdersResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);
    }
}