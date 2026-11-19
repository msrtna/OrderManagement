
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.ShoppingCarts.ClearCart;

public sealed class ClearCartHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClearCartHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ClearCartResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                "UserNotAuthenticated",
                "The current user is not authenticated.");
        }

        var shoppingCart = await _context.ShoppingCarts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);

        if (shoppingCart is null || shoppingCart.Items.Count == 0)
        {
            return Success(0);
        }

        var removedItemsCount = shoppingCart.Items.Count;

        _context.CartItems.RemoveRange(shoppingCart.Items);

        await _context.SaveChangesAsync(cancellationToken);

        return Success(removedItemsCount);
    }

    private static ClearCartResult Success(int removedItemsCount)
    {
        return new ClearCartResult(
            true,
            new ClearCartResponse(removedItemsCount),
            null,
            null);
    }

    private static ClearCartResult Failure(
        string errorCode,
        string errorMessage)
    {
        return new ClearCartResult(
            false,
            null,
            errorCode,
            errorMessage);
    }
}
