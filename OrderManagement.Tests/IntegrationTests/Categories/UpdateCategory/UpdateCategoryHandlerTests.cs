
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Categories.UpdateCategory;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Categories.UpdateCategory;

public sealed class UpdateCategoryHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private UpdateCategoryHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new UpdateCategoryHandler(
            _context,
            new UpdateCategoryValidator());
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateCategory_WhenRequestIsValid()
    {
        // Arrange
        var category = new Category
        {
            Name = "Old Name",
            Description = "Old Description"
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new UpdateCategoryRequest(
            category.Id,
            "New Name",
            "New Description");

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var updatedCategory = await _context.Categories
            .FindAsync(category.Id);

        Assert.NotNull(updatedCategory);
        Assert.Equal("New Name", updatedCategory.Name);
        Assert.Equal("New Description", updatedCategory.Description);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = new UpdateCategoryRequest(
            999,
            "New Name",
            null);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode == "CategoryNotFound");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenCategoryIsInactive()
    {
        // Arrange
        var category = new Category
        {
            Name = "Inactive Category",
            IsActive = false
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new UpdateCategoryRequest(
            category.Id,
            "New Name",
            null);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode == "CategoryNotFound");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenNameAlreadyExists()
    {
        // Arrange
        var firstCategory = new Category { Name = "Electronics" };
        var secondCategory = new Category { Name = "Books" };

        _context.Categories.AddRange(firstCategory, secondCategory);
        await _context.SaveChangesAsync();

        var request = new UpdateCategoryRequest(
            secondCategory.Id,
            "Electronics",
            null);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode == "DuplicateCategoryName");
    }

    [Fact]
    public async Task HandleAsync_ShouldAllowKeepingSameName()
    {
        // Arrange
        var category = new Category
        {
            Name = "Electronics"
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new UpdateCategoryRequest(
            category.Id,
            "Electronics",
            null);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task HandleAsync_ShouldTrimNameAndDescription()
    {
        // Arrange
        var category = new Category
        {
            Name = "Old Name"
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new UpdateCategoryRequest(
            category.Id,
            "  New Name  ",
            "  New Description  ");

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.True(result.Succeeded);

        var updatedCategory = await _context.Categories
            .FindAsync(category.Id);

        Assert.NotNull(updatedCategory);
        Assert.Equal("New Name", updatedCategory.Name);
        Assert.Equal("New Description", updatedCategory.Description);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationErrors_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new UpdateCategoryRequest(
            0,
            "",
            null);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task HandleAsync_ShouldCheckDuplicateNameAgainstInactiveCategories()
    {
        // Arrange
        var activeCategory = new Category
        {
            Name = "Books"
        };

        var inactiveCategory = new Category
        {
            Name = "Electronics",
            IsActive = false
        };

        _context.Categories.AddRange(activeCategory, inactiveCategory);
        await _context.SaveChangesAsync();

        var request = new UpdateCategoryRequest(
            activeCategory.Id,
            "Electronics",
            null);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode == "DuplicateCategoryName");
    }
}
