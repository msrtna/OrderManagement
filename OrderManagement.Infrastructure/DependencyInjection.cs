
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Identity;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddHttpContextAccessor();

        services.AddDataProtection();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Issuer),
                "JWT Issuer is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Audience),
                "JWT Audience is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.SecretKey)
                    && Encoding.UTF8.GetByteCount(options.SecretKey) >= 32,
                "JWT SecretKey must contain at least 32 bytes.")
            .Validate(
                options => options.AccessTokenExpirationMinutes > 0,
                "Access token expiration must be greater than zero.")
            .Validate(
                options => options.RefreshTokenExpirationDays > 0,
                "Refresh token expiration must be greater than zero.")
            .ValidateOnStart();

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
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
