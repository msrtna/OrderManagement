using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;

public sealed class UpdateCartItemQuantityHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<UpdateCartItemQuantityRequest> _validator;

    public UpdateCartItemQuantityHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IValidator<UpdateCartItemQuantityRequest> validator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    public async Task<UpdateCartItemQuantityResult> HandleAsync(
        long cartItemId,
        UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new UpdateCartItemQuantityError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new UpdateCartItemQuantityResult(
                false,
                null,
                errors);
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                "UserNotAuthenticated",
                "The current user is not authenticated.");
        }

        var cartItem = await _context.CartItems
            .Include(item => item.ShoppingCart)
            .Include(item => item.Product)
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

        if (cartItem.IsProductDeleted ||
            cartItem.ProductId is null ||
            cartItem.Product is null ||
            !cartItem.Product.IsActive)
        {
            return Failure(
                "ProductNotAvailable",
                "The product is no longer available.");
        }

        var storeSettings = await _context.StoreSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                settings => settings.Id == 1,
                cancellationToken);

        if (storeSettings is null)
        {
            return Failure(
                "StoreSettingsNotFound",
                "Store settings could not be found.");
        }

        if (request.Quantity > storeSettings.MaxCartItemQuantity)
        {
            return Failure(
                "MaximumCartItemQuantityExceeded",
                $"The maximum allowed quantity for a product is {storeSettings.MaxCartItemQuantity}.");
        }

        cartItem.Quantity = request.Quantity;

        await _context.SaveChangesAsync(cancellationToken);

        var response = new UpdateCartItemQuantityResponse(
            cartItem.Id,
            cartItem.ProductId.Value,
            cartItem.ProductName,
            cartItem.Quantity);

        return new UpdateCartItemQuantityResult(
            true,
            response,
            Array.Empty<UpdateCartItemQuantityError>());
    }

    private static UpdateCartItemQuantityResult Failure(
        string errorCode,
        string errorMessage)
    {
        return new UpdateCartItemQuantityResult(
            false,
            null,
            new[]
            {
                new UpdateCartItemQuantityError(
                    errorCode,
                    errorMessage)
            });
    }
}
