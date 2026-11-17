
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Features.Categories.GetCategories;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Categories.GetCategories;

public sealed class GetCategoriesHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private GetCategoriesHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _handler = new GetCategoriesHandler(
            _context,
            new GetCategoriesValidator());
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnActiveCategories()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { Name = "Electronics", IsActive = true },
            new Category { Name = "Books", IsActive = true },
            new Category { Name = "Inactive", IsActive = false });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(
            new GetCategoriesRequest());

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(2, result.Response.TotalCount);
        Assert.Equal(2, result.Response.Items.Count);
        Assert.DoesNotContain(
            result.Response.Items,
            category => category.Name == "Inactive");
    }

    [Fact]
    public async Task HandleAsync_ShouldApplyPagination()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { Name = "Category A" },
            new Category { Name = "Category B" },
            new Category { Name = "Category C" },
            new Category { Name = "Category D" });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(
            new GetCategoriesRequest(
                PageNumber: 2,
                PageSize: 2));

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(4, result.Response.TotalCount);
        Assert.Equal(2, result.Response.TotalPages);
        Assert.Equal(2, result.Response.Items.Count);
        Assert.Equal(
            new[] { "Category C", "Category D" },
            result.Response.Items.Select(x => x.Name).ToArray());
    }

    [Fact]
    public async Task HandleAsync_ShouldApplySearchTerm()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { Name = "Electronics" },
            new Category { Name = "Electronic Accessories" },
            new Category { Name = "Books" });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(
            new GetCategoriesRequest(SearchTerm: "Electronic"));

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(2, result.Response.TotalCount);
        Assert.Equal(2, result.Response.Items.Count);
        Assert.All(
            result.Response.Items,
            item => Assert.Contains(
                "Electronic",
                item.Name,
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsync_ShouldSortDescending_WhenRequested()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { Name = "Books" },
            new Category { Name = "Clothing" },
            new Category { Name = "Electronics" });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(
            new GetCategoriesRequest(SortDescending: true));

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(
            new[] { "Electronics", "Clothing", "Books" },
            result.Response.Items.Select(x => x.Name).ToArray());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationErrors_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new GetCategoriesRequest(
            PageNumber: 0,
            PageSize: 10);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyResult_WhenNoCategoriesMatch()
    {
        // Arrange
        _context.Categories.Add(
            new Category { Name = "Books" });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(
            new GetCategoriesRequest(SearchTerm: "Electronics"));

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Empty(result.Response.Items);
        Assert.Equal(0, result.Response.TotalCount);
        Assert.Equal(0, result.Response.TotalPages);
    }
}
