
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.UserAddresses.CreateAddress;
using OrderManagement.Application.Features.UserAddresses.DeleteAddress;
using OrderManagement.Application.Features.UserAddresses.GetAddresses;
using OrderManagement.Application.Features.UserAddresses.GetAddressById;
using OrderManagement.Application.Features.UserAddresses.SetDefaultAddress;
using OrderManagement.Application.Features.UserAddresses.UpdateAddress;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
public sealed class AddressesController : ControllerBase
{
    private readonly CreateAddressHandler _createAddressHandler;
    private readonly GetAddressesHandler _getAddressesHandler;
    private readonly GetAddressByIdHandler _getAddressByIdHandler;
    private readonly UpdateAddressHandler _updateAddressHandler;
    private readonly SetDefaultAddressHandler _setDefaultAddressHandler;
    private readonly DeleteAddressHandler _deleteAddressHandler;

    public AddressesController(
        CreateAddressHandler createAddressHandler,
        GetAddressesHandler getAddressesHandler,
        GetAddressByIdHandler getAddressByIdHandler,
        UpdateAddressHandler updateAddressHandler,
        SetDefaultAddressHandler setDefaultAddressHandler,
        DeleteAddressHandler deleteAddressHandler)
    {
        _createAddressHandler = createAddressHandler;
        _getAddressesHandler = getAddressesHandler;
        _getAddressByIdHandler = getAddressByIdHandler;
        _updateAddressHandler = updateAddressHandler;
        _setDefaultAddressHandler = setDefaultAddressHandler;
        _deleteAddressHandler = deleteAddressHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAddressResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createAddressHandler.HandleAsync(request, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors });

        return StatusCode(StatusCodes.Status201Created, result.Response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetAddressesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _getAddressesHandler.HandleAsync(cancellationToken);

        if (result is null)
            return Unauthorized();

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(GetAddressByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await _getAddressByIdHandler.HandleAsync(id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(UpdateAddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateAddressHandler.HandleAsync(id, request, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorCode == "AddressNotFound")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });

            if (result.ErrorCode == "UserNotAuthenticated")
                return Unauthorized();

            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return Ok(result.Response);
    }

    [HttpPut("{id:long}/default")]
    [ProducesResponseType(typeof(SetDefaultAddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetDefault(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _setDefaultAddressHandler.HandleAsync(id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(DeleteAddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _deleteAddressHandler.HandleAsync(id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
