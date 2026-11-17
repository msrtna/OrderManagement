using OrderManagement.Application.Features.Products.UpdateProduct;

namespace OrderManagement.Tests.UnitTests.Features.Products.UpdateProduct;

public sealed class UpdateProductValidatorTests
{
    private readonly UpdateProductValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = new UpdateProductRequest(
            Name: "Laptop",
            Description: "Gaming laptop",
            Price: 1500,
            StockQuantity: 10,
            CategoryId: 1);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        var request = CreateValidRequest(Name: "");

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.Name));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameContainsOnlyWhitespace()
    {
        var request = CreateValidRequest(Name: "   ");

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.Name));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceedsMaximumLength()
    {
        var request = CreateValidRequest(
            Name: new string('a', 201));

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.Name));
    }

    [Fact]
    public void Validate_ShouldPass_WhenNameIsAtMaximumLength()
    {
        var request = CreateValidRequest(
            Name: new string('a', 200));

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var request = CreateValidRequest(
            Description: new string('a', 2001));

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.Description));
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionIsNull()
    {
        var request = CreateValidRequest(Description: null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionIsAtMaximumLength()
    {
        var request = CreateValidRequest(
            Description: new string('a', 2000));

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPriceIsNegative()
    {
        var request = CreateValidRequest(Price: -1);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.Price));
    }

    [Fact]
    public void Validate_ShouldPass_WhenPriceIsZero()
    {
        var request = CreateValidRequest(Price: 0);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenStockQuantityIsNegative()
    {
        var request = CreateValidRequest(StockQuantity: -1);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.StockQuantity));
    }

    [Fact]
    public void Validate_ShouldPass_WhenStockQuantityIsZero()
    {
        var request = CreateValidRequest(StockQuantity: 0);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenCategoryIdIsInvalid(int categoryId)
    {
        var request = CreateValidRequest(CategoryId: categoryId);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateProductRequest.CategoryId));
    }

    private static UpdateProductRequest CreateValidRequest(
        string Name = "Laptop",
        string? Description = "Gaming laptop",
        decimal Price = 1500,
        int StockQuantity = 10,
        int CategoryId = 1)
    {
        return new UpdateProductRequest(
            Name,
            Description,
            Price,
            StockQuantity,
            CategoryId);
    }
}
