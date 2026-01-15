using Microsoft.Extensions.DependencyInjection;
using SmartTracker.Application.Services;
using SmartTracker.Application.ServicesInterfaces;

namespace SmartTracker.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();

            return services;
        }
    }
}
