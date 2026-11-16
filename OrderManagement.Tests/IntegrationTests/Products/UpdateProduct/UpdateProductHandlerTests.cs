
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Products.UpdateProduct;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Products.UpdateProduct;

public sealed class UpdateProductHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private UpdateProductHandler _handler = null!;

    private int _categoryId;
    private int _productId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new UpdateProductHandler(
            _context,
            new UpdateProductValidator());

        await SeedDataAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateProduct_WhenRequestIsValid()
    {
        var request = new UpdateProductRequest(
            "Updated Laptop",
            "Updated Gaming Laptop",
            2000,
            15,
            _categoryId);

        var result = await _handler.HandleAsync(
            _productId,
            request);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var product = await _context.Products
            .SingleAsync(product => product.Id == _productId);

        Assert.Equal("Updated Laptop", product.Name);
        Assert.Equal("Updated Gaming Laptop", product.Description);
        Assert.Equal(2000, product.Price);
        Assert.Equal(15, product.StockQuantity);
        Assert.Equal(_categoryId, product.CategoryId);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductDoesNotExist()
    {
        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(
            999,
            request);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotFound");
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenProductIsInactive()
    {
        var product = await _context.Products
            .SingleAsync(product => product.Id == _productId);

        product.IsActive = false;
        await _context.SaveChangesAsync();

        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(
            _productId,
            request);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "ProductNotFound");

        Assert.Equal(
            "Laptop",
            (await _context.Products
                .SingleAsync(product => product.Id == _productId)).Name);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCategoryDoesNotExist()
    {
        var request = CreateValidRequest(CategoryId: 999);

        var result = await _handler.HandleAsync(
            _productId,
            request);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "CategoryNotFoundOrInactive");

        var product = await _context.Products
            .SingleAsync(product => product.Id == _productId);

        Assert.Equal("Laptop", product.Name);
        Assert.Equal(1500, product.Price);
        Assert.Equal(_categoryId, product.CategoryId);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCategoryIsInactive()
    {
        var category = await _context.Categories
            .SingleAsync(category => category.Id == _categoryId);

        category.IsActive = false;
        await _context.SaveChangesAsync();

        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(
            _productId,
            request);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "CategoryNotFoundOrInactive");

        var product = await _context.Products
            .SingleAsync(product => product.Id == _productId);

        Assert.Equal("Laptop", product.Name);
        Assert.Equal(1500, product.Price);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var request = new UpdateProductRequest(
            "",
            null,
            -100,
            -1,
            0);

        var result = await _handler.HandleAsync(
            _productId,
            request);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        var product = await _context.Products
            .SingleAsync(product => product.Id == _productId);

        Assert.Equal("Laptop", product.Name);
        Assert.Equal(1500, product.Price);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(_categoryId, product.CategoryId);
    }

    private UpdateProductRequest CreateValidRequest(
        string Name = "Updated Laptop",
        string? Description = "Updated description",
        decimal Price = 2000,
        int StockQuantity = 15,
        int? CategoryId = null)
    {
        return new UpdateProductRequest(
            Name,
            Description,
            Price,
            StockQuantity,
            CategoryId ?? _categoryId);
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

        _categoryId = category.Id;

        var product = new Product
        {
            Name = "Laptop",
            Description = "Gaming Laptop",
            Price = 1500,
            StockQuantity = 10,
            CategoryId = _categoryId,
            IsActive = true
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        _productId = product.Id;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
