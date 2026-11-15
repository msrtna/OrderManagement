using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderManagement.Api;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Tests.IntegrationTests.Identity;

namespace OrderManagement.Tests.IntegrationTests.Authentication;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "OrderManagement.Tests",
                ["Jwt:Audience"] = "OrderManagement.Tests.Client",
                ["Jwt:SecretKey"] =
                    "OrderManagement_Test_Secret_Key_123456789",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };

            configuration.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<IdentityServiceTestDbContext>(
                options => options.UseSqlite(_connection));

            services.AddScoped<ApplicationDbContext>(provider =>
                provider.GetRequiredService<IdentityServiceTestDbContext>());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
