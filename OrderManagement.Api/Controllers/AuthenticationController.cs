using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.Authentication.Register;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly RegisterHandler _registerHandler;

    public AuthenticationController(RegisterHandler registerHandler)
    {
        _registerHandler = registerHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            var isDuplicate = result.Errors.Any(error =>
                error.Code == "DuplicateEmail" ||
                error.Code == "DuplicateUserName");

            if (isDuplicate)
            {
                return Conflict(new
                {
                    Errors = result.Errors
                });
            }

            return BadRequest(new
            {
                Errors = result.Errors
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Response);
    }
}