
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Products.DeactivateProduct;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Products.DeactivateProduct;

public sealed class DeactivateProductHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private DeactivateProductHandler _handler = null!;

    private int _productId;
    private int _secondProductId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new DeactivateProductHandler(_context);

        await SeedDataAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldDeactivateProduct_WhenProductIsActive()
    {
        var result = await _handler.HandleAsync(_productId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var product = await _context.Products
            .SingleAsync(product => product.Id == _productId);

        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductDoesNotExist()
    {
        var result = await _handler.HandleAsync(999);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotFound");
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductIsAlreadyInactive()
    {
        var product = await _context.Products
            .SingleAsync(product => product.Id == _productId);

        product.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(_productId);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotFound");
    }

    [Fact]
    public async Task HandleAsync_ShouldNotDeleteProduct_Physically()
    {
        var result = await _handler.HandleAsync(_productId);

        Assert.True(result.Succeeded);

        var product = await _context.Products
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(product => product.Id == _productId);

        Assert.NotNull(product);
        Assert.False(product.IsActive);

        Assert.Equal(2, await _context.Products.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotModifyOtherProducts()
    {
        var secondProductBefore = await _context.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == _secondProductId);

        var result = await _handler.HandleAsync(_productId);

        Assert.True(result.Succeeded);

        var secondProductAfter = await _context.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == _secondProductId);

        Assert.Equal(secondProductBefore.Name, secondProductAfter.Name);
        Assert.Equal(secondProductBefore.Price, secondProductAfter.Price);
        Assert.Equal(
            secondProductBefore.StockQuantity,
            secondProductAfter.StockQuantity);
        Assert.Equal(
            secondProductBefore.IsActive,
            secondProductAfter.IsActive);
    }

    private async Task SeedDataAsync()
    {
        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic products",
            IsActive = true
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var products = new List<Product>
        {
            new()
            {
                Name = "Laptop",
                Description = "Gaming Laptop",
                Price = 1500,
                StockQuantity = 10,
                CategoryId = category.Id,
                IsActive = true
            },
            new()
            {
                Name = "Mouse",
                Description = "Wireless Mouse",
                Price = 50,
                StockQuantity = 30,
                CategoryId = category.Id,
                IsActive = true
            }
        };

        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();

        _productId = products[0].Id;
        _secondProductId = products[1].Id;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
