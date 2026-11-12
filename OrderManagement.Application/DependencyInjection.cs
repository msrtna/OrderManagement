using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Features.Authentication.Register;

namespace OrderManagement.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
            services.AddScoped<RegisterHandler>();

            return services;
        }
    }
}
