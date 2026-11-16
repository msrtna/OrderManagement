using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Application.Features.Products.GetProductById;

namespace OrderManagement.Tests.IntegrationTests.Products.GetProductById;

public sealed class GetProductByIdHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetProductByIdHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new GetProductByIdHandler(_context);

        await SeedDataAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnProduct_WhenProductExistsAndIsActive()
    {
        var product = await _context.Products.SingleAsync();

        var result = await _handler.HandleAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("Electronics", result.CategoryName);
        Assert.Equal(1500, result.Price);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        var result = await _handler.HandleAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenProductIsInactive()
    {
        var product = await _context.Products.SingleAsync();

        product.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(product.Id);

        Assert.Null(result);
    }

    private async Task SeedDataAsync()
    {
        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic products",
            IsActive = true
        };

        var product = new Product
        {
            Name = "Laptop",
            Description = "Gaming Laptop",
            Price = 1500,
            StockQuantity = 10,
            IsActive = true,
            Category = category
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}