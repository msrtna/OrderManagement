using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Identity;

public static class DevelopmentAdminSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        var email = configuration["DevelopmentAdmin:Email"];
        var password = configuration["DevelopmentAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Development Admin credentials are not configured.");
        }

        using var scope = serviceProvider.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Development",
                LastName = "Admin"
            };

            var createResult = await userManager.CreateAsync(
                user,
                password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to create Development Admin: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(
                user,
                "Admin");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to assign Admin role: {errors}");
            }
        }
    }
}