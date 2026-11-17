using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Products.GetProducts;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Products.GetProducts;

public sealed class GetProductsHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetProductsHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new GetProductsHandler(_context);

        await SeedDataAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllActiveProducts()
    {
        var request = new GetProductsRequest();

        var result = await _handler.HandleAsync(request);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task HandleAsync_ShouldApplyPagination()
    {
        var request = new GetProductsRequest(
            PageNumber: 2,
            PageSize: 2);

        var result = await _handler.HandleAsync(request);

        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterBySearchTerm()
    {
        var request = new GetProductsRequest(
            SearchTerm: "Laptop");

        var result = await _handler.HandleAsync(request);

        Assert.Single(result.Items);
        Assert.Equal("Laptop", result.Items.First().Name);
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterByCategoryId()
    {
        var category = await _context.Categories
            .FirstAsync(category => category.Name == "Electronics");

        var request = new GetProductsRequest(
            CategoryId: category.Id);

        var result = await _handler.HandleAsync(request);

        Assert.Equal(2, result.TotalCount);
        Assert.All(
            result.Items,
            item => Assert.Equal(category.Id, item.CategoryId));
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterByPriceRange()
    {
        var request = new GetProductsRequest(
            MinPrice: 100,
            MaxPrice: 1000);

        var result = await _handler.HandleAsync(request);

        Assert.Single(result.Items);
        Assert.Equal("Mouse", result.Items.First().Name);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotReturnInactiveProducts()
    {
        var inactiveProduct = new Product
        {
            Name = "Inactive Product",
            Price = 100,
            StockQuantity = 5,
            CategoryId = 1,
            IsActive = false
        };

        await _context.Products.AddAsync(inactiveProduct);
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(new GetProductsRequest());

        Assert.Equal(3, result.TotalCount);
        Assert.DoesNotContain(
            result.Items,
            item => item.Name == "Inactive Product");
    }

    private async Task SeedDataAsync()
    {
        var electronics = new Category
        {
            Name = "Electronics",
            IsActive = true
        };

        var accessories = new Category
        {
            Name = "Accessories",
            IsActive = true
        };

        await _context.Categories.AddRangeAsync(
            electronics,
            accessories);

        await _context.SaveChangesAsync();

        var products = new List<Product>
        {
            new()
            {
                Name = "Laptop",
                Price = 1500,
                StockQuantity = 5,
                CategoryId = electronics.Id,
                IsActive = true
            },
            new()
            {
                Name = "Mouse",
                Price = 500,
                StockQuantity = 20,
                CategoryId = electronics.Id,
                IsActive = true
            },
            new()
            {
                Name = "Keyboard",
                Price = 1200,
                StockQuantity = 10,
                CategoryId = accessories.Id,
                IsActive = true
            }
        };

        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}