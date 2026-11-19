
using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Checkout.PayOrder;

public sealed class PayOrderHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<PayOrderRequest> _validator;

    public PayOrderHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IValidator<PayOrderRequest> validator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    public async Task<PayOrderResult> HandleAsync(
        PayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new PayOrderError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToArray();

            return new PayOrderResult(false, null, errors);
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                "UserNotAuthenticated",
                "The current user is not authenticated.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var now = DateTime.UtcNow;

            var order = await _context.Orders
                .Include(order => order.Items)
                .FirstOrDefaultAsync(
                    order => order.Id == request.OrderId &&
                             order.UserId == userId,
                    cancellationToken);

            if (order is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "OrderNotFound",
                    "The specified order does not exist or does not belong to the current user.");
            }

            if (order.Status != OrderStatus.PendingPayment)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "OrderNotPayable",
                    "The order is not in PendingPayment status.");
            }

            if (order.ExpiresAt <= now)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "OrderExpired",
                    "The order has expired and cannot be paid.");
            }

            if (!order.Items.Any())
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "OrderHasNoItems",
                    "The order does not contain any items.");
            }

            var hasActiveAttempt = await _context.PaymentAttempts
                .AnyAsync(
                    attempt =>
                        attempt.OrderId == order.Id &&
                        (attempt.Status == PaymentAttemptStatus.Pending ||
                         attempt.Status == PaymentAttemptStatus.Unknown),
                    cancellationToken);

            if (hasActiveAttempt)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "PaymentAlreadyInProgress",
                    "A payment attempt is already in progress for this order.");
            }

            var storeSettings = await _context.StoreSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    settings => settings.Id == 1,
                    cancellationToken);

            if (storeSettings is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "StoreSettingsNotFound",
                    "Store settings could not be found.");
            }

            if (order.Items.Any(item =>
                    item.ProductId is null ||
                    item.Quantity <= 0 ||
                    item.UnitPrice < 0 ||
                    item.TotalPrice != item.UnitPrice * item.Quantity))
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "InvalidOrderItems",
                    "One or more order items contain invalid data.");
            }

            var groupedItems = order.Items
                .GroupBy(item => item.ProductId!.Value)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToArray();

            var attemptNumber = await _context.PaymentAttempts
                .Where(attempt => attempt.OrderId == order.Id)
                .Select(attempt => (int?)attempt.AttemptNumber)
                .MaxAsync(cancellationToken) ?? 0;

            var paymentAttempt = new PaymentAttempt
            {
                OrderId = order.Id,
                AttemptNumber = attemptNumber + 1,
                Amount = order.TotalAmount,
                Status = PaymentAttemptStatus.Pending,
                StartedAt = now,
                CreatedAt = now
            };

            await _context.PaymentAttempts.AddAsync(
                paymentAttempt,
                cancellationToken);

            var reservationExpiresAt = now.AddMinutes(
                storeSettings.ReservationDurationMinutes);

            foreach (var item in groupedItems)
            {
                var affectedRows = await _context.Products
                    .Where(product =>
                        product.Id == item.ProductId &&
                        product.IsActive &&
                        product.StockQuantity >= item.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            product => product.StockQuantity,
                            product => product.StockQuantity - item.Quantity),
                        cancellationToken);

                if (affectedRows != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Failure(
                        "InsufficientStockOrInactiveProduct",
                        $"Product with ID '{item.ProductId}' is inactive or does not have sufficient stock.");
                }

                var reservation = new StockReservation
                {
                    PaymentAttempt = paymentAttempt,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ExpiresAt = reservationExpiresAt,
                    Status = StockReservationStatus.Consumed,
                    CompletedAt = now,
                    CreatedAt = now
                };

                paymentAttempt.StockReservations.Add(reservation);
            }

            var cart = await _context.ShoppingCarts
                .Include(cart => cart.Items)
                .FirstOrDefaultAsync(
                    cart => cart.UserId == userId,
                    cancellationToken);

            if (cart is not null && cart.Items.Any())
            {
                _context.CartItems.RemoveRange(cart.Items);
            }

            paymentAttempt.Status = PaymentAttemptStatus.Succeeded;
            paymentAttempt.CompletedAt = now;

            order.Status = OrderStatus.Paid;
            order.UpdatedAt = now;

            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderStatus.Paid,
                ChangedByUserId = userId,
                Description = "Payment completed successfully.",
                CreatedAt = now
            };

            await _context.OrderStatusHistories.AddAsync(
                statusHistory,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var response = new PayOrderResponse(
                order.Id,
                order.Status.ToString(),
                paymentAttempt.Id,
                paymentAttempt.Status.ToString(),
                paymentAttempt.Amount,
                paymentAttempt.CompletedAt.Value);

            return new PayOrderResult(
                true,
                response,
                Array.Empty<PayOrderError>());
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static PayOrderResult Failure(
        string code,
        string description)
    {
        return new PayOrderResult(
            false,
            null,
            new[]
            {
                new PayOrderError(code, description)
            });
    }
}
