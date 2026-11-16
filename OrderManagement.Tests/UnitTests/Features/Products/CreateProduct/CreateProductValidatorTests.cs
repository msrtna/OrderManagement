using OrderManagement.Application.Features.Products.CreateProduct;

namespace OrderManagement.Tests.UnitTests.Features.Products.CreateProduct;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = new CreateProductRequest(
            "Laptop",
            "Gaming Laptop",
            1500,
            10,
            1);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenNameIsEmpty(string name)
    {
        var request = new CreateProductRequest(
            name,
            "Description",
            100,
            10,
            1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.Name));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceedsMaximumLength()
    {
        var request = new CreateProductRequest(
            new string('A', 201),
            null,
            100,
            10,
            1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var request = new CreateProductRequest(
            "Laptop",
            new string('A', 2001),
            100,
            10,
            1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPriceIsNegative()
    {
        var request = new CreateProductRequest(
            "Laptop",
            null,
            -1,
            10,
            1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenStockQuantityIsNegative()
    {
        var request = new CreateProductRequest(
            "Laptop",
            null,
            100,
            -1,
            1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCategoryIdIsInvalid()
    {
        var request = new CreateProductRequest(
            "Laptop",
            null,
            100,
            10,
            0);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}