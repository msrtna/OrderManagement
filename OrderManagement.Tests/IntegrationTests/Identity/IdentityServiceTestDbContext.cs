using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.IntegrationTests.Identity;

public sealed class IdentityServiceTestDbContext : ApplicationDbContext
{
    public IdentityServiceTestDbContext(
        DbContextOptions options) : base(options)
    {
    }

    protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        // Intentionally skip production configurations.
        // SQLite will use its own type mappings.
    }
}