
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Features.Categories.CreateCategory;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Categories.CreateCategory;

public sealed class CreateCategoryHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private CreateCategoryHandler _handler = null!;

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

        services.AddValidatorsFromAssemblyContaining<CreateCategoryValidator>();

        var serviceProvider = services.BuildServiceProvider();

        var validator = serviceProvider
            .GetRequiredService<IValidator<CreateCategoryRequest>>();

        _handler = new CreateCategoryHandler(_context, validator);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateCategory_WhenRequestIsValid()
    {
        var request = new CreateCategoryRequest(
            "Electronics",
            "Electronic products");

        var result = await _handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("Electronics", result.Response.Name);
        Assert.Equal("Electronic products", result.Response.Description);
        Assert.True(result.Response.IsActive);
        Assert.True(result.Response.Id > 0);

        var category = await _context.Categories.SingleAsync();

        Assert.Equal(result.Response.Id, category.Id);
        Assert.Equal("Electronics", category.Name);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCategoryNameAlreadyExists()
    {
        var existingCategory = new Domain.Entities.Category
        {
            Name = "Electronics",
            Description = "Existing category",
            IsActive = true
        };

        await _context.Categories.AddAsync(existingCategory);
        await _context.SaveChangesAsync();

        var request = new CreateCategoryRequest(
            "Electronics",
            "Another category");

        var result = await _handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == "DuplicateCategoryName");

        Assert.Single(await _context.Categories.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRequestIsInvalid()
    {
        var request = new CreateCategoryRequest(
            "",
            new string('A', 501));

        var result = await _handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.NotEmpty(result.Errors);

        Assert.Empty(await _context.Categories.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_ShouldTrimNameAndDescription_WhenRequestIsValid()
    {
        var request = new CreateCategoryRequest(
            "  Electronics  ",
            "  Electronic products  ");

        var result = await _handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal("Electronics", result.Response.Name);
        Assert.Equal("Electronic products", result.Response.Description);

        var category = await _context.Categories.SingleAsync();

        Assert.Equal("Electronics", category.Name);
        Assert.Equal("Electronic products", category.Description);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
