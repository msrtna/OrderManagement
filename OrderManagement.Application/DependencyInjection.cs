using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Features.Authentication.Login;
using OrderManagement.Application.Features.Authentication.RefreshToken;
using OrderManagement.Application.Features.Authentication.Register;
using OrderManagement.Application.Features.Categories.CreateCategory;
using OrderManagement.Application.Features.Categories.DeactivateCategory;
using OrderManagement.Application.Features.Categories.GetCategories;
using OrderManagement.Application.Features.Categories.GetCategoryById;
using OrderManagement.Application.Features.Categories.UpdateCategory;
using OrderManagement.Application.Features.Checkout.CreateOrder;
using OrderManagement.Application.Features.Checkout.PayOrder;
using OrderManagement.Application.Features.Products.CreateProduct;
using OrderManagement.Application.Features.Products.DeactivateProduct;
using OrderManagement.Application.Features.Products.GetProductById;
using OrderManagement.Application.Features.Products.GetProducts;
using OrderManagement.Application.Features.Products.UpdateProduct;
using OrderManagement.Application.Features.ShoppingCarts.AddToCart;
using OrderManagement.Application.Features.ShoppingCarts.ClearCart;
using OrderManagement.Application.Features.ShoppingCarts.GetCart;
using OrderManagement.Application.Features.ShoppingCarts.RemoveCartItem;
using OrderManagement.Application.Features.ShoppingCarts.UpdateCartItemQuantity;
using OrderManagement.Application.Features.UserAddresses.CreateAddress;
using OrderManagement.Application.Features.UserAddresses.DeleteAddress;
using OrderManagement.Application.Features.UserAddresses.GetAddressById;
using OrderManagement.Application.Features.UserAddresses.GetAddresses;
using OrderManagement.Application.Features.UserAddresses.SetDefaultAddress;
using OrderManagement.Application.Features.UserAddresses.UpdateAddress;

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

        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<GetCategoryByIdHandler>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<DeactivateCategoryHandler>();

        services.AddScoped<AddToCartHandler>();
        services.AddScoped<GetCartHandler>();
        services.AddScoped<UpdateCartItemQuantityHandler>();
        services.AddScoped<RemoveCartItemHandler>();
        services.AddScoped<ClearCartHandler>();

        services.AddScoped<CreateAddressHandler>();
        services.AddScoped<GetAddressesHandler>();
        services.AddScoped<GetAddressByIdHandler>();
        services.AddScoped<UpdateAddressHandler>();
        services.AddScoped<SetDefaultAddressHandler>();
        services.AddScoped<DeleteAddressHandler>();

        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<PayOrderHandler>();

        return services;
    }
}