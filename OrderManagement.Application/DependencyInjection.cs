using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Authentication.Login;
using OrderManagement.Application.Features.Authentication.RefreshToken;
using OrderManagement.Application.Features.Authentication.Register;
using OrderManagement.Application.Features.Products.CreateProduct;
using OrderManagement.Application.Features.Products.DeactivateProduct;
using OrderManagement.Application.Features.Products.GetProductById;
using OrderManagement.Application.Features.Products.GetProducts;
using OrderManagement.Application.Features.Products.UpdateProduct;

namespace OrderManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Issuer),
                "JWT Issuer is required.")
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Audience),
                "JWT Audience is required.")
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.SecretKey)
                && System.Text.Encoding.UTF8.GetByteCount(options.SecretKey) >= 32,
                "JWT SecretKey must contain at least 32 bytes.")
            .Validate(options =>
                options.AccessTokenExpirationMinutes > 0,
                "Access token expiration must be greater than zero.")
            .Validate(options =>
                options.RefreshTokenExpirationDays > 0,
                "Refresh token expiration must be greater than zero.");

        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();

        services.AddScoped<CreateProductHandler>();
        services.AddScoped<GetProductByIdHandler>();
        services.AddScoped<GetProductsHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<DeactivateProductHandler>();

        return services;
    }
}