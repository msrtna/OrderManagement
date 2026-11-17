
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Products.GetProducts;
using OrderManagement.Domain.Entities;
using OrderManagement.Tests.IntegrationTests.Fixtures;

namespace OrderManagement.Tests.IntegrationTests.Products.GetProducts;

public sealed class GetProductsSqlServerTests
    : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;

    public GetProductsSqlServerTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HandleAsync_ShouldSortByPriceDescending()
    {
        var context = _fixture.Context;

        var category = new Category
        {
            Name = "Electronics",
            IsActive = true
        };

        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var products = new List<Product>
        {
            new()
            {
                Name = "Laptop",
                Price = 1500,
                StockQuantity = 5,
                CategoryId = category.Id,
                IsActive = true
            },
            new()
            {
                Name = "Mouse",
                Price = 500,
                StockQuantity = 20,
                CategoryId = category.Id,
                IsActive = true
            },
            new()
            {
                Name = "Keyboard",
                Price = 1200,
                StockQuantity = 10,
                CategoryId = category.Id,
                IsActive = true
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        var handler = new GetProductsHandler(context);

        var request = new GetProductsRequest(
            SortBy: "Price",
            SortDescending: true);

        var result = await handler.HandleAsync(request);

        Assert.Equal(3, result.TotalCount);

        Assert.Equal(
            new[] { "Laptop", "Keyboard", "Mouse" },
            result.Items.Select(item => item.Name).ToArray());
    }
}
