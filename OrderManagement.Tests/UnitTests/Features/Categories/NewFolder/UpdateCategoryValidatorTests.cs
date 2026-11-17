
using OrderManagement.Application.Features.Categories.UpdateCategory;

namespace OrderManagement.Tests.UnitTests.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = new UpdateCategoryRequest(
            Id: 1,
            Name: "Electronics",
            Description: "Electronic products");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenIdIsInvalid(int id)
    {
        var request = new UpdateCategoryRequest(
            Id: id,
            Name: "Electronics",
            Description: null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(request.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldFail_WhenNameIsEmpty(string name)
    {
        var request = new UpdateCategoryRequest(
            Id: 1,
            Name: name,
            Description: null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(request.Name));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceedsMaximumLength()
    {
        var request = new UpdateCategoryRequest(
            Id: 1,
            Name: new string('a', 151),
            Description: null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(request.Name));
    }

    [Fact]
    public void Validate_ShouldPass_WhenNameHasMaximumLength()
    {
        var request = new UpdateCategoryRequest(
            Id: 1,
            Name: new string('a', 150),
            Description: null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var request = new UpdateCategoryRequest(
            Id: 1,
            Name: "Electronics",
            Description: new string('a', 501));

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(request.Description));
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionHasMaximumLength()
    {
        var request = new UpdateCategoryRequest(
            Id: 1,
            Name: "Electronics",
            Description: new string('a', 500));

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionIsNull()
    {
        var request = new UpdateCategoryRequest(
            Id: 1,
            Name: "Electronics",
            Description: null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
