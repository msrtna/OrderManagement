
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.Checkout.CreateOrder;
using OrderManagement.Application.Features.Checkout.PayOrder;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/checkout")]
[Authorize]
public sealed class CheckoutController : ControllerBase
{
    private readonly CreateOrderHandler _createOrderHandler;
    private readonly PayOrderHandler _payOrderHandler;

    public CheckoutController(
        CreateOrderHandler createOrderHandler,
        PayOrderHandler payOrderHandler)
    {
        _createOrderHandler = createOrderHandler;
        _payOrderHandler = payOrderHandler;
    }

    [HttpPost("orders")]
    [ProducesResponseType(
        typeof(CreateOrderResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createOrderHandler.HandleAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Errors = result.Errors
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Response);
    }

    [HttpPost("orders/{orderId:long}/pay")]
    [ProducesResponseType(
        typeof(PayOrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PayOrder(
        long orderId,
        CancellationToken cancellationToken)
    {
        var result = await _payOrderHandler.HandleAsync(
            new PayOrderRequest(orderId),
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
}
