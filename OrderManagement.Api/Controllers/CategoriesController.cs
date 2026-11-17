using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.Categories.CreateCategory;
using OrderManagement.Application.Features.Categories.DeactivateCategory;
using OrderManagement.Application.Features.Categories.GetCategories;
using OrderManagement.Application.Features.Categories.GetCategoryById;
using OrderManagement.Application.Features.Categories.UpdateCategory;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize(Roles = "Admin")]
public sealed class CategoriesController : ControllerBase
{
    private readonly CreateCategoryHandler _createCategoryHandler;
    private readonly GetCategoryByIdHandler _getCategoryByIdHandler;
    private readonly GetCategoriesHandler _getCategoriesHandler;
    private readonly UpdateCategoryHandler _updateCategoryHandler;
    private readonly DeactivateCategoryHandler _deactivateCategoryHandler;

    public CategoriesController(
        CreateCategoryHandler createCategoryHandler,
        GetCategoryByIdHandler getCategoryByIdHandler,
        GetCategoriesHandler getCategoriesHandler,
        UpdateCategoryHandler updateCategoryHandler,
        DeactivateCategoryHandler deactivateCategoryHandler)
    {
        _createCategoryHandler = createCategoryHandler;
        _getCategoryByIdHandler = getCategoryByIdHandler;
        _getCategoriesHandler = getCategoriesHandler;
        _updateCategoryHandler = updateCategoryHandler;
        _deactivateCategoryHandler = deactivateCategoryHandler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateCategoryResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createCategoryHandler.HandleAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Errors = result.Errors
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Response!.Id },
            result.Response);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(GetCategoryByIdResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _getCategoryByIdHandler.HandleAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(GetCategoriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
    [FromQuery] GetCategoriesRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _getCategoriesHandler.HandleAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors });
        }

        return Ok(result.Response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
    int id,
    [FromBody] UpdateCategoryRequest request,
    CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(new
            {
                Error = "Route ID does not match request ID."
            });
        }

        var result = await _updateCategoryHandler.HandleAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deactivate(
    int id,
    CancellationToken cancellationToken)
    {
        var result = await _deactivateCategoryHandler.HandleAsync(
            id,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors });
        }

        return NoContent();
    }
}
