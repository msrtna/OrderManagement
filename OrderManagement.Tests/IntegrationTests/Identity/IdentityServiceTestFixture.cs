using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Identity;
using OrderManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace OrderManagement.Tests.IntegrationTests.Identity;

public sealed class IdentityServiceTestFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public ApplicationDbContext DbContext { get; }
    public UserManager<ApplicationUser> UserManager { get; }
    public RoleManager<IdentityRole> RoleManager { get; }
    public IdentityService IdentityService { get; }

    private IdentityServiceTestFixture(
        SqliteConnection connection,
        ServiceProvider serviceProvider,
        IServiceScope scope)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _scope = scope;

        DbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        UserManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        RoleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        IdentityService = new IdentityService(
            UserManager,
            RoleManager,
            DbContext);
    }

    public static async Task<IdentityServiceTestFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<IdentityServiceTestDbContext>(options =>
            options.UseSqlite(connection));

        // Register the derived context as the production context type
        // because IdentityService expects ApplicationDbContext.
        services.AddScoped<ApplicationDbContext>(provider =>
            provider.GetRequiredService<IdentityServiceTestDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;

            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<IdentityServiceTestDbContext>();

        var serviceProvider = services.BuildServiceProvider();

        var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        return new IdentityServiceTestFixture(
            connection,
            serviceProvider,
            scope);
    }

    public async Task SeedCustomerRoleAsync()
    {
        if (await RoleManager.RoleExistsAsync("Customer"))
            return;

        var result = await RoleManager.CreateAsync(
            new IdentityRole("Customer"));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not create Customer role.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();

        await _serviceProvider.DisposeAsync();

        await _connection.DisposeAsync();
    }
}
