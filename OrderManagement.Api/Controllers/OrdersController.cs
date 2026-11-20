using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.Orders.GetMyOrders;
using OrderManagement.Application.Features.Orders.GetOrderById;
using OrderManagement.Application.Features.Orders.GetOrderStatusHistory;
using OrderManagement.Application.Features.Orders.GetShipmentByOrderId;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly GetMyOrdersHandler _getMyOrdersHandler;
    private readonly IValidator<GetMyOrdersRequest> _getMyOrdersValidator;
    private readonly GetOrderByIdHandler _getOrderByIdHandler;
    private readonly IValidator<GetOrderByIdRequest> _getOrderByIdValidator;
    private readonly GetOrderStatusHistoryHandler _getOrderStatusHistoryHandler;
    private readonly IValidator<GetOrderStatusHistoryRequest> _getOrderStatusHistoryValidator;
    private readonly GetShipmentByOrderIdHandler _getShipmentByOrderIdHandler;
    private readonly GetShipmentByOrderIdValidator _getShipmentByOrderIdValidator;

    public OrdersController(
        GetMyOrdersHandler getMyOrdersHandler,
        IValidator<GetMyOrdersRequest> getMyOrdersValidator,
        GetOrderByIdHandler getOrderByIdHandler,
        IValidator<GetOrderByIdRequest> getOrderByIdValidator,
        GetOrderStatusHistoryHandler getOrderStatusHistoryHandler,
        IValidator<GetOrderStatusHistoryRequest> getOrderStatusHistoryValidator,
        GetShipmentByOrderIdHandler getShipmentByOrderIdHandler,
        GetShipmentByOrderIdValidator getShipmentByOrderIdValidator)
    {
        _getMyOrdersHandler = getMyOrdersHandler;
        _getMyOrdersValidator = getMyOrdersValidator;
        _getOrderByIdHandler = getOrderByIdHandler;
        _getOrderByIdValidator = getOrderByIdValidator;
        _getOrderStatusHistoryHandler = getOrderStatusHistoryHandler;
        _getOrderStatusHistoryValidator = getOrderStatusHistoryValidator;
        _getShipmentByOrderIdHandler = getShipmentByOrderIdHandler;
        _getShipmentByOrderIdValidator = getShipmentByOrderIdValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetMyOrdersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] GetMyOrdersRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _getMyOrdersValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Errors = validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage
                })
            });
        }

        var result = await _getMyOrdersHandler.Handle(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(GetOrderByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
    long id,
    CancellationToken cancellationToken)
    {
        var request = new GetOrderByIdRequest(id);

        var validationResult = await _getOrderByIdValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Errors = validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage
                })
            });
        }

        var result = await _getOrderByIdHandler.Handle(
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{id:long}/status-history")]
    [ProducesResponseType(typeof(GetOrderStatusHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderStatusHistory(
    long id,
    CancellationToken cancellationToken)
    {
        var request = new GetOrderStatusHistoryRequest(id);

        var validationResult = await _getOrderStatusHistoryValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Errors = validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage
                })
            });
        }

        var result = await _getOrderStatusHistoryHandler.Handle(
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{id:long}/shipment")]
    [Authorize]
    public async Task<IActionResult> GetShipmentByOrderId(
    long id,
    CancellationToken cancellationToken)
    {
        var request = new GetShipmentByOrderIdRequest(id);

        var validationResult = await _getShipmentByOrderIdValidator
            .ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var result = await _getShipmentByOrderIdHandler
            .Handle(request, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
