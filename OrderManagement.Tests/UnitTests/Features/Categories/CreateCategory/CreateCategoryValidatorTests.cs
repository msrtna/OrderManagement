
using OrderManagement.Application.Features.Categories.CreateCategory;

namespace OrderManagement.Tests.UnitTests.Features.Categories.CreateCategory;

public sealed class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = new CreateCategoryRequest(
            "Electronics",
            "Electronic products");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        var request = new CreateCategoryRequest(
            "",
            "Electronic products");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameContainsOnlyWhitespace()
    {
        var request = new CreateCategoryRequest(
            "   ",
            null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceedsMaximumLength()
    {
        var request = new CreateCategoryRequest(
            new string('A', 151),
            null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ShouldPass_WhenNameHasMaximumLength()
    {
        var request = new CreateCategoryRequest(
            new string('A', 150),
            null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var request = new CreateCategoryRequest(
            "Electronics",
            new string('A', 501));

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Description");
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionIsNull()
    {
        var request = new CreateCategoryRequest(
            "Electronics",
            null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionHasMaximumLength()
    {
        var request = new CreateCategoryRequest(
            "Electronics",
            new string('A', 500));

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
