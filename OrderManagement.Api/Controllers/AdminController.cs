
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetDashboard()
    {
        return Ok(new
        {
            Message = "Welcome to the Admin Dashboard.",
            AccessLevel = "Admin"
        });
    }
}
