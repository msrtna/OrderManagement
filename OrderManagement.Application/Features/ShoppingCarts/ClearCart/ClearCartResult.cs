namespace OrderManagement.Application.Features.ShoppingCarts.ClearCart;

public sealed record ClearCartResult(
    bool Succeeded,
    ClearCartResponse? Response,
    string? ErrorCode,
    string? ErrorMessage);