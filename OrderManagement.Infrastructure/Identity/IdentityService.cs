using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            CanPurchase = true,
            LockoutEnabled = true
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

    public async Task<LoginIdentityResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null || user.IsBlocked)
        {
            return new LoginIdentityResult(
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                new IdentityError(
                    "InvalidCredentials",
                    "Invalid email or password.")
                });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return new LoginIdentityResult(
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                new IdentityError(
                    "AccountLocked",
                    "Account is temporarily locked.")
                });
        }

        var passwordIsValid = await _userManager.CheckPasswordAsync(
            user,
            password);

        if (!passwordIsValid)
        {
            await _userManager.AccessFailedAsync(user);

            return new LoginIdentityResult(
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                new IdentityError(
                    "InvalidCredentials",
                    "Invalid email or password.")
                });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return new LoginIdentityResult(
            true,
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            roles.ToArray(),
            Array.Empty<IdentityError>());
    }

    public async Task StoreRefreshTokenAsync(
    string userId,
    string tokenHash,
    DateTime expiresAt,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.RefreshTokens.AddAsync(
            refreshToken,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }



    public async Task<RefreshTokenIdentityResult> RotateRefreshTokenAsync(
        string tokenHash,
        string newTokenHash,
        DateTime newExpiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var existingToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (existingToken is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new RefreshTokenIdentityResult(
                false,
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                new IdentityError(
                    "InvalidRefreshToken",
                    "Refresh token is invalid.")
                });
        }

        var user = existingToken.User;

        if (existingToken.RevokedAt.HasValue)
        {
            // A previously revoked token was reused.
            // Revoke all active refresh tokens belonging to this user.

            var activeTokens = await _dbContext.RefreshTokens
                .Where(x =>
                    x.UserId == existingToken.UserId &&
                    x.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var revokedAt = DateTime.UtcNow;

            foreach (var token in activeTokens)
            {
                token.RevokedAt = revokedAt;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RefreshTokenIdentityResult(
                false,
                true,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                new IdentityError(
                    "RefreshTokenReuseDetected",
                    "Refresh token reuse was detected. All active refresh tokens have been revoked.")
                });
        }

        if (existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new RefreshTokenIdentityResult(
                false,
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                new IdentityError(
                    "RefreshTokenExpired",
                    "Refresh token has expired.")
                });
        }

        if (user.IsBlocked)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new RefreshTokenIdentityResult(
                false,
                false,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                new[]
                {
                new IdentityError(
                    "InvalidRefreshToken",
                    "The user account is not allowed to refresh tokens.")
                });
        }

        var roles = await _userManager.GetRolesAsync(user);

        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByTokenHash = newTokenHash;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = newExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.RefreshTokens.AddAsync(
            newRefreshToken,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new RefreshTokenIdentityResult(
            true,
            false,
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            roles.ToArray(),
            Array.Empty<IdentityError>());
    }

}
