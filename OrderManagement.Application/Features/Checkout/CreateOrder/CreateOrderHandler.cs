
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Checkout.CreateOrder;

public sealed class CreateOrderHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateOrderRequest> _validator;

    public CreateOrderHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IValidator<CreateOrderRequest> validator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    public async Task<CreateOrderResult> HandleAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new CreateOrderError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToArray();

            return new CreateOrderResult(false, null, errors);
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                "UserNotAuthenticated",
                "The current user is not authenticated.");
        }

        var address = await _context.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                address => address.Id == request.AddressId &&
                           address.UserId == userId,
                cancellationToken);

        if (address is null)
        {
            return Failure(
                "AddressNotFound",
                "The specified address does not exist or does not belong to the current user.");
        }

        var shoppingCart = await _context.ShoppingCarts
            .Include(cart => cart.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);

        if (shoppingCart is null || !shoppingCart.Items.Any())
        {
            return Failure(
                "CartNotFoundOrEmpty",
                "The shopping cart is empty.");
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

        var orderItems = new List<OrderItem>();
        var totalItems = 0;
        decimal itemsTotal = 0;

        foreach (var cartItem in shoppingCart.Items)
        {
            if (cartItem.Quantity <= 0 ||
                cartItem.Quantity > storeSettings.MaxCartItemQuantity)
            {
                return Failure(
                    "InvalidCartItem",
                    "One or more cart items have an invalid quantity.");
            }

            var product = cartItem.Product;

            if (cartItem.IsProductDeleted ||
                cartItem.ProductId is null ||
                product is null ||
                !product.IsActive)
            {
                return Failure(
                    "ProductNotFoundOrInactive",
                    $"Product '{cartItem.ProductName}' does not exist or is inactive.");
            }

            if (product.StockQuantity < cartItem.Quantity)
            {
                return Failure(
                    "InsufficientStock",
                    $"Insufficient stock for product '{product.Name}'.");
            }

            var lineTotal = product.Price * cartItem.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                UnitPrice = product.Price,
                TotalPrice = lineTotal
            });

            totalItems += cartItem.Quantity;
            itemsTotal += lineTotal;
        }

        var shippingCost = storeSettings.DefaultShippingCost;
        var totalAmount = itemsTotal + shippingCost;
        var now = DateTime.UtcNow;

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            ShippingCost = shippingCost,
            TotalAmount = totalAmount,
            ExpiresAt = now.AddHours(
                storeSettings.OrderExpirationHours),

            RecipientName = address.RecipientName,
            PhoneNumber = address.PhoneNumber,
            Province = address.Province,
            City = address.City,
            AddressLine = address.AddressLine,
            PostalCode = address.PostalCode,

            Items = orderItems
        };

        await _context.Orders.AddAsync(
            order,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CreateOrderResponse(
            order.Id,
            order.Status.ToString(),
            totalItems,
            itemsTotal,
            shippingCost,
            totalAmount,
            order.ExpiresAt);

        return new CreateOrderResult(
            true,
            response,
            Array.Empty<CreateOrderError>());
    }

    private static CreateOrderResult Failure(
        string code,
        string description)
    {
        return new CreateOrderResult(
            false,
            null,
            new[]
            {
                new CreateOrderError(code, description)
            });
    }
}
