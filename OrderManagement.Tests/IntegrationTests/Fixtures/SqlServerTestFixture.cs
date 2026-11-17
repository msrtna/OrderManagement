
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Fixtures;

public sealed class SqlServerTestFixture : IAsyncLifetime
{
    private readonly string _connectionString;

    public ApplicationDbContext Context { get; private set; } = null!;

    public SqlServerTestFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        _connectionString = configuration
            .GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "Test database connection string was not found.");
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        Context = new ApplicationDbContext(options);

        await Context.Database.EnsureDeletedAsync();
        await Context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}
