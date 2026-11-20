using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.Shipments.GetShipmentStatusHistory;
using OrderManagement.Application.Features.Shipments.UpdateShipmentStatus;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/shipments")]
public sealed class ShipmentsController : ControllerBase
{
    private readonly UpdateShipmentStatusHandler _handler;
    private readonly IValidator<UpdateShipmentStatusRequest> _validator;
    private readonly GetShipmentStatusHistoryHandler _historyHandler;
    private readonly IValidator<GetShipmentStatusHistoryRequest> _historyValidator;

    public ShipmentsController(
        UpdateShipmentStatusHandler handler,
        IValidator<UpdateShipmentStatusRequest> validator,
        GetShipmentStatusHistoryHandler historyHandler,
        IValidator<GetShipmentStatusHistoryRequest> historyValidator)
    {
        _handler = handler;
        _validator = validator;
        _historyHandler = historyHandler;
        _historyValidator = historyValidator;
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateShipmentStatus(
        long id,
        [FromBody] UpdateShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with
        {
            ShipmentId = id
        };

        var validationResult = await _validator.ValidateAsync(
            updateRequest,
            cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            var result = await _handler.Handle(
                updateRequest,
                cancellationToken);

            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:long}/status-history")]
    [Authorize]
    public async Task<IActionResult> GetShipmentStatusHistory(
        long id,
        CancellationToken cancellationToken)
    {
        var request = new GetShipmentStatusHistoryRequest(id);

        var validationResult = await _historyValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var result = await _historyHandler.Handle(
            request,
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
