using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.Authentication.Login;
using OrderManagement.Application.Features.Authentication.Register;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly RegisterHandler _registerHandler;
    private readonly LoginHandler _loginHandler;

    public AuthenticationController(RegisterHandler registerHandler, LoginHandler loginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            var isAuthenticationFailure = result.Errors.Any(error =>
                error.Code is "InvalidCredentials" or "AccountLocked");

            if (isAuthenticationFailure)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            return BadRequest(new
            {
                errors = result.Errors
            });
        }

        return Ok(result.Response);
    }
}