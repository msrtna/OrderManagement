
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.ShoppingCarts.GetCart;

public sealed class GetCartHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCartHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetCartResponse?> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var shoppingCart = await _context.ShoppingCarts
            .AsNoTracking()
            .Include(cart => cart.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);

        if (shoppingCart is null)
        {
            return new GetCartResponse(
                0,
                Array.Empty<GetCartItemResponse>(),
                0,
                0);
        }

        var items = shoppingCart.Items
            .Select(item =>
            {
                var unitPrice = item.Product is { IsActive: true }
                    ? item.Product.Price
                    : 0m;

                return new GetCartItemResponse(
                    item.Id,
                    item.ProductId,
                    item.ProductName,
                    unitPrice,
                    item.Quantity,
                    unitPrice * item.Quantity,
                    item.IsProductDeleted ||
                    item.Product is null ||
                    !item.Product.IsActive);
            })
            .ToList();

        var totalItems = items.Sum(item => item.Quantity);

        var totalAmount = items.Sum(item => item.LineTotal);

        return new GetCartResponse(
            shoppingCart.Id,
            items,
            totalItems,
            totalAmount);
    }
}
