
using OrderManagement.Application.Features.Categories.GetCategories;

namespace OrderManagement.Tests.UnitTests.Features.Categories.GetCategories;

public sealed class GetCategoriesValidatorTests
{
    private readonly GetCategoriesValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = new GetCategoriesRequest(
            PageNumber: 1,
            PageSize: 10,
            SearchTerm: "Electronics");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenSearchTermIsNull()
    {
        var request = new GetCategoriesRequest(
            PageNumber: 1,
            PageSize: 10,
            SearchTerm: null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenSearchTermIsEmpty()
    {
        var request = new GetCategoriesRequest(
            PageNumber: 1,
            PageSize: 10,
            SearchTerm: "");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPageNumberIsInvalid(int pageNumber)
    {
        var request = new GetCategoriesRequest(
            PageNumber: pageNumber,
            PageSize: 10);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(request.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_ShouldFail_WhenPageSizeIsInvalid(int pageSize)
    {
        var request = new GetCategoriesRequest(
            PageNumber: 1,
            PageSize: pageSize);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(request.PageSize));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_ShouldPass_WhenPageSizeIsAtBoundary(int pageSize)
    {
        var request = new GetCategoriesRequest(
            PageNumber: 1,
            PageSize: pageSize);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSearchTermExceedsMaximumLength()
    {
        var request = new GetCategoriesRequest(
            PageNumber: 1,
            PageSize: 10,
            SearchTerm: new string('a', 101));

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(request.SearchTerm));
    }

    [Fact]
    public void Validate_ShouldPass_WhenSearchTermHasMaximumLength()
    {
        var request = new GetCategoriesRequest(
            PageNumber: 1,
            PageSize: 10,
            SearchTerm: new string('a', 100));

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
