
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.Orders.GetOrderStatusHistory;

public sealed class GetOrderStatusHistoryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderStatusHistoryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetOrderStatusHistoryResponse?> Handle(
        GetOrderStatusHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var orderExists = await _context.Orders
            .AsNoTracking()
            .AnyAsync(
                order => order.Id == request.OrderId &&
                         order.UserId == userId,
                cancellationToken);

        if (!orderExists)
        {
            return null;
        }

        var histories = await _context.OrderStatusHistories
            .AsNoTracking()
            .Where(history => history.OrderId == request.OrderId)
            .OrderBy(history => history.CreatedAt)
            .ThenBy(history => history.Id)
            .Select(history => new OrderStatusHistoryItemResponse(
                history.Id,
                history.Status,
                history.Description,
                history.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetOrderStatusHistoryResponse(
            request.OrderId,
            histories);
    }
}
