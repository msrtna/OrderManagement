using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Features.Products.CreateProduct;
using OrderManagement.Application.Features.Products.DeactivateProduct;
using OrderManagement.Application.Features.Products.GetProductById;
using OrderManagement.Application.Features.Products.GetProducts;
using OrderManagement.Application.Features.Products.UpdateProduct;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = "Admin")]
public sealed class ProductsController : ControllerBase
{
    private readonly CreateProductHandler _createProductHandler;
    private readonly GetProductByIdHandler _getProductByIdHandler;
    private readonly GetProductsHandler _getProductsHandler;
    private readonly UpdateProductHandler _updateProductHandler;
    private readonly DeactivateProductHandler _deactivateProductHandler;
    private readonly IValidator<GetProductsRequest> _getProductsValidator;

    public ProductsController(
        CreateProductHandler createProductHandler,
        GetProductByIdHandler getProductByIdHandler,
        GetProductsHandler getProductsHandler,
        UpdateProductHandler updateProductHandler,
        DeactivateProductHandler deactivateProductHandler,
        IValidator<GetProductsRequest> getProductsValidator)
    {
        _createProductHandler = createProductHandler;
        _getProductByIdHandler = getProductByIdHandler;
        _getProductsHandler = getProductsHandler;
        _updateProductHandler = updateProductHandler;
        _deactivateProductHandler = deactivateProductHandler;
        _getProductsValidator = getProductsValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createProductHandler.HandleAsync(
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

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
    int id,
    [FromBody] UpdateProductRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _updateProductHandler.HandleAsync(
            id,
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Errors = result.Errors
            });
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetProductByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _getProductByIdHandler.HandleAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(GetProductsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetProductsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _getProductsValidator.ValidateAsync(
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

        var result = await _getProductsHandler.HandleAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deactivate(
    int id,
    CancellationToken cancellationToken)
    {
        var result = await _deactivateProductHandler.HandleAsync(
            id,
            cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(new
            {
                Errors = result.Errors
            });
        }

        return NoContent();
    }
}