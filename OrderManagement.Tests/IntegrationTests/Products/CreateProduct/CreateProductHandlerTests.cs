using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Features.Products.CreateProduct;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Products.CreateProduct;

public sealed class CreateProductHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private CreateProductHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        var services = new ServiceCollection();

        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

        var serviceProvider = services.BuildServiceProvider();

        var validator = serviceProvider
            .GetRequiredService<
                FluentValidation.IValidator<CreateProductRequest>>();

        _handler = new CreateProductHandler(_context, validator);

        await SeedCategoryAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateProduct_WhenRequestIsValid()
    {
        var request = new CreateProductRequest(
            "Laptop",
            "Gaming Laptop",
            1500,
            10,
            1);

        var result = await _handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal("Laptop", result.Response.Name);
        Assert.Equal(1500, result.Response.Price);
        Assert.Equal(10, result.Response.StockQuantity);
        Assert.Equal(1, result.Response.CategoryId);
        Assert.True(result.Response.IsActive);

        var product = await _context.Products.SingleAsync();

        Assert.Equal("Laptop", product.Name);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCategoryDoesNotExist()
    {
        var request = new CreateProductRequest(
            "Laptop",
            null,
            1500,
            10,
            999);

        var result = await _handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "CategoryNotFoundOrInactive");

        Assert.Empty(await _context.Products.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCategoryIsInactive()
    {
        var category = await _context.Categories.SingleAsync();
        category.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new CreateProductRequest(
            "Laptop",
            null,
            1500,
            10,
            category.Id);

        var result = await _handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "CategoryNotFoundOrInactive");

        Assert.Empty(await _context.Products.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var request = new CreateProductRequest(
            "",
            null,
            -100,
            -1,
            1);

        var result = await _handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.NotEmpty(result.Errors);

        Assert.Empty(await _context.Products.ToListAsync());
    }

    private async Task SeedCategoryAsync()
    {
        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic products",
            IsActive = true
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}