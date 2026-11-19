using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.ShoppingCarts.RemoveCartItem;

public sealed class RemoveCartItemHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveCartItemHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RemoveCartItemResult> HandleAsync(
        long cartItemId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                "UserNotAuthenticated",
                "The current user is not authenticated.");
        }

        var cartItem = await _context.CartItems
            .Include(item => item.ShoppingCart)
            .FirstOrDefaultAsync(
                item => item.Id == cartItemId &&
                        item.ShoppingCart.UserId == userId,
                cancellationToken);

        if (cartItem is null)
        {
            return Failure(
                "CartItemNotFound",
                "The specified cart item was not found.");
        }

        var response = new RemoveCartItemResponse(
            cartItem.Id,
            cartItem.ProductName);

        _context.CartItems.Remove(cartItem);

        await _context.SaveChangesAsync(cancellationToken);

        return new RemoveCartItemResult(
            true,
            response,
            Array.Empty<RemoveCartItemError>());
    }

    private static RemoveCartItemResult Failure(
        string errorCode,
        string errorMessage)
    {
        return new RemoveCartItemResult(
            false,
            null,
            new[]
            {
                new RemoveCartItemError(
                    errorCode,
                    errorMessage)
            });
    }
}