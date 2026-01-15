using SmartTracker.Application.Common;

namespace SmartTracker.API.DependencyInjection
{
    public static class ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services)
        {
            services.AddScoped<RequestFilterContext>();

            return services;
        }
    }
}
