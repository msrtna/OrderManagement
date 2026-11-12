using Microsoft.AspNetCore.Identity;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using IdentityError = OrderManagement.Application.Abstractions.Identity.IdentityError;

namespace OrderManagement.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private const string CustomerRole = "Customer";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<RegisterResult> RegisterCustomerAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roleExists = await _roleManager.RoleExistsAsync(CustomerRole);

        if (!roleExists)
        {
            return new RegisterResult(
                false,
                null,
                new[]
                {
                    new IdentityError(
                        "CustomerRoleNotConfigured",
                        "Customer role has not been configured.")
                });
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim(),
            UserName = email.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsBlocked = false,
            CanPurchase = true
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new RegisterResult(
                false,
                null,
                createResult.Errors
                .Select(error => new IdentityError(
                error.Code,
                error.Description))
                .ToArray());
        }

        var roleResult =
            await _userManager.AddToRoleAsync(user, CustomerRole);

        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new RegisterResult(
                false,
                null,
                roleResult.Errors
                    .Select(error => new IdentityError(
                        error.Code,
                        error.Description))
                    .ToArray());
        }

        await transaction.CommitAsync(cancellationToken);

        return new RegisterResult(
            true,
            user.Id,
            Array.Empty<IdentityError>());
    }
}
