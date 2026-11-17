
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Categories.DeactivateCategory;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Categories.DeactivateCategory;

public sealed class DeactivateCategoryHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private DeactivateCategoryHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new DeactivateCategoryHandler(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldDeactivateCategory_WhenCategoryExists()
    {
        // Arrange
        var category = new Category
        {
            Name = "Electronics",
            IsActive = true
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(category.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var updatedCategory = await _context.Categories
            .FindAsync(category.Id);

        Assert.NotNull(updatedCategory);
        Assert.False(updatedCategory.IsActive);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _handler.HandleAsync(999);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode == "CategoryNotFound");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenCategoryIsAlreadyInactive()
    {
        // Arrange
        var category = new Category
        {
            Name = "Inactive Category",
            IsActive = false
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(category.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode == "CategoryNotFound");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HandleAsync_ShouldReturnError_WhenIdIsInvalid(int id)
    {
        // Act
        var result = await _handler.HandleAsync(id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode == "InvalidCategoryId");
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepCategoryInDatabase_WhenDeactivated()
    {
        // Arrange
        var category = new Category
        {
            Name = "Books",
            IsActive = true
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        await _handler.HandleAsync(category.Id);

        // Assert
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == category.Id);

        Assert.True(categoryExists);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotDeleteRelatedProducts_WhenDeactivated()
    {
        // Arrange
        var category = new Category
        {
            Name = "Electronics",
            IsActive = true
        };

        var product = new Product
        {
            Name = "Laptop",
            Price = 1000,
            StockQuantity = 5,
            Category = category,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(category.Id);

        // Assert
        Assert.True(result.Succeeded);

        var savedProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        Assert.NotNull(savedProduct);
        Assert.True(savedProduct.IsActive);
        Assert.Equal(category.Id, savedProduct.CategoryId);

        var savedCategory = await _context.Categories
            .FindAsync(category.Id);

        Assert.NotNull(savedCategory);
        Assert.False(savedCategory.IsActive);
    }
}
