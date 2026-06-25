using DealTrack.Application.Common;
using DealTrack.API.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.DependencyInjection
{
    public static class ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services)
        {
            services.AddScoped<RequestFilterContext>();

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add<ApiResponseStatusCodeFilter>();
            });

            return services;
        }
    }
}
