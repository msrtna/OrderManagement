using OrderManagement.Application.Features.Products.GetProducts;

namespace OrderManagement.Tests.UnitTests.Features.Products.GetProducts;

public sealed class GetProductsValidatorTests
{
    private readonly GetProductsValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = new GetProductsRequest(
            PageNumber: 1,
            PageSize: 10,
            SearchTerm: "Laptop",
            CategoryId: 1,
            MinPrice: 100,
            MaxPrice: 2000,
            SortBy: "Name",
            SortDescending: false);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPageNumberIsInvalid(int pageNumber)
    {
        var request = new GetProductsRequest(
            PageNumber: pageNumber);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPageSizeIsInvalid(int pageSize)
    {
        var request = new GetProductsRequest(
            PageSize: pageSize);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.PageSize));
    }

    [Fact]
    public void Validate_ShouldPass_WhenPageSizeIsAtMinimum()
    {
        var request = new GetProductsRequest(PageSize: 1);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenPageSizeIsAtMaximum()
    {
        var request = new GetProductsRequest(PageSize: 100);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSearchTermExceedsMaximumLength()
    {
        var request = new GetProductsRequest(
            SearchTerm: new string('a', 101));

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.SearchTerm));
    }

    [Fact]
    public void Validate_ShouldPass_WhenSearchTermIsNull()
    {
        var request = new GetProductsRequest(SearchTerm: null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenCategoryIdIsInvalid(int categoryId)
    {
        var request = new GetProductsRequest(
            CategoryId: categoryId);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.CategoryId));
    }

    [Fact]
    public void Validate_ShouldPass_WhenCategoryIdIsNull()
    {
        var request = new GetProductsRequest(CategoryId: null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMinPriceIsNegative()
    {
        var request = new GetProductsRequest(MinPrice: -1);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.MinPrice));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxPriceIsNegative()
    {
        var request = new GetProductsRequest(MaxPrice: -1);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.MaxPrice));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMinPriceExceedsMaxPrice()
    {
        var request = new GetProductsRequest(
            MinPrice: 1000,
            MaxPrice: 500);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage ==
                "Minimum price cannot exceed maximum price.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenMinPriceEqualsMaxPrice()
    {
        var request = new GetProductsRequest(
            MinPrice: 500,
            MaxPrice: 500);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("Price")]
    [InlineData("price")]
    [InlineData("PRICE")]
    public void Validate_ShouldPass_WhenSortByIsSupported(string sortBy)
    {
        var request = new GetProductsRequest(
            SortBy: sortBy);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Date")]
    [InlineData("Stock")]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldFail_WhenSortByIsUnsupported(string sortBy)
    {
        var request = new GetProductsRequest(
            SortBy: sortBy);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.SortBy));
    }

    [Fact]
    public void Validate_ShouldFail_WhenSortByIsNull()
    {
        var request = new GetProductsRequest(
            SortBy: null!);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetProductsRequest.SortBy));
    }
}
