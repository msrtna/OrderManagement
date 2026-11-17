
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Categories.GetCategoryById;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Categories.GetCategoryById;

public sealed class GetCategoryByIdHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetCategoryByIdHandler _handler = null!;

    private int _categoryId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new GetCategoryByIdHandler(_context);

        await SeedCategoryAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCategory_WhenCategoryExistsAndIsActive()
    {
        var result = await _handler.HandleAsync(_categoryId);

        Assert.NotNull(result);
        Assert.Equal(_categoryId, result.Id);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("Electronic products", result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        var result = await _handler.HandleAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenCategoryIsInactive()
    {
        var category = await _context.Categories
            .SingleAsync(category => category.Id == _categoryId);

        category.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _handler.HandleAsync(_categoryId);

        Assert.Null(result);
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

        _categoryId = category.Id;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
