
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Features.ShoppingCarts.AddToCart;

public sealed class AddToCartHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<AddToCartRequest> _validator;

    public AddToCartHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IValidator<AddToCartRequest> validator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    public async Task<AddToCartResult> HandleAsync(
        AddToCartRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new AddToCartError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new AddToCartResult(
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

        var product = await _context.Products
            .FirstOrDefaultAsync(
                product => product.Id == request.ProductId &&
                           product.IsActive,
                cancellationToken);

        if (product is null)
        {
            return Failure(
                "ProductNotFoundOrInactive",
                "The specified product does not exist or is inactive.");
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

        var shoppingCart = await _context.ShoppingCarts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);

        var existingCartItem = shoppingCart?.Items
            .FirstOrDefault(
                item => item.ProductId == product.Id &&
                        !item.IsProductDeleted);

        var newQuantity = request.Quantity;

        if (existingCartItem is not null)
        {
            newQuantity += existingCartItem.Quantity;
        }

        if (newQuantity > storeSettings.MaxCartItemQuantity)
        {
            return Failure(
                "MaximumCartItemQuantityExceeded",
                $"The maximum allowed quantity for a product is {storeSettings.MaxCartItemQuantity}.");
        }

        if (shoppingCart is null)
        {
            shoppingCart = new ShoppingCart
            {
                UserId = userId
            };

            await _context.ShoppingCarts.AddAsync(
                shoppingCart,
                cancellationToken);
        }

        CartItem cartItem;

        if (existingCartItem is not null)
        {
            existingCartItem.Quantity = newQuantity;
            cartItem = existingCartItem;
        }
        else
        {
            cartItem = new CartItem
            {
                ShoppingCart = shoppingCart,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = request.Quantity,
                IsProductDeleted = false
            };

            await _context.CartItems.AddAsync(
                cartItem,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = new AddToCartResponse(
            cartItem.Id,
            product.Id,
            cartItem.ProductName,
            cartItem.Quantity);

        return new AddToCartResult(
            true,
            response,
            Array.Empty<AddToCartError>());
    }

    private static AddToCartResult Failure(
        string errorCode,
        string errorMessage)
    {
        return new AddToCartResult(
            false,
            null,
            new[]
            {
                new AddToCartError(errorCode, errorMessage)
            });
    }
}
