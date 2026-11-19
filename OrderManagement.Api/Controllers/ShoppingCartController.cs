
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.ShoppingCarts.AddToCart;
using OrderManagement.Application.Features.ShoppingCarts.ClearCart;
using OrderManagement.Application.Features.ShoppingCarts.GetCart;
using OrderManagement.Application.Features.ShoppingCarts.RemoveCartItem;
using OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/shopping-cart")]
[Authorize]
public sealed class ShoppingCartController : ControllerBase
{
    private readonly AddToCartHandler _addToCartHandler;
    private readonly GetCartHandler _getCartHandler;
    private readonly UpdateCartItemQuantityHandler _updateCartItemQuantityHandler;
    private readonly RemoveCartItemHandler _removeCartItemHandler;
    private readonly ClearCartHandler _clearCartHandler;

    public ShoppingCartController(
        AddToCartHandler addToCartHandler,
        GetCartHandler getCartHandler,
        UpdateCartItemQuantityHandler updateCartItemQuantityHandler,
        RemoveCartItemHandler removeCartItemHandler,
        ClearCartHandler clearCartHandler)
    {
        _addToCartHandler = addToCartHandler;
        _getCartHandler = getCartHandler;
        _updateCartItemQuantityHandler = updateCartItemQuantityHandler;
        _removeCartItemHandler = removeCartItemHandler;
        _clearCartHandler = clearCartHandler;
    }

    [HttpPost("items")]
    [ProducesResponseType(
        typeof(AddToCartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddToCartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addToCartHandler.HandleAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Errors = result.Errors
            });
        }

        return Ok(result.Response);
    }

    [HttpGet]
    [ProducesResponseType(
    typeof(GetCartResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCart(
    CancellationToken cancellationToken)
    {
        var result = await _getCartHandler.HandleAsync(
            cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpPut("items/{cartItemId:long}")]
    [ProducesResponseType(
    typeof(UpdateCartItemQuantityResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateItemQuantity(
    long cartItemId,
    [FromBody] UpdateCartItemQuantityRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _updateCartItemQuantityHandler.HandleAsync(
            cartItemId,
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Errors = result.Errors
            });
        }

        return Ok(result.Response);
    }

    [HttpDelete("items/{cartItemId:long}")]
    [ProducesResponseType(
    typeof(RemoveCartItemResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveItem(
    long cartItemId,
    CancellationToken cancellationToken)
    {
        var result = await _removeCartItemHandler.HandleAsync(
            cartItemId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Errors = result.Errors
            });
        }

        return Ok(result.Response);
    }

    [HttpDelete]
    [ProducesResponseType(
    typeof(ClearCartResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCart(
    CancellationToken cancellationToken)
    {
        var result = await _clearCartHandler.HandleAsync(
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage
            });
        }

        return Ok(result.Response);
    }
}
