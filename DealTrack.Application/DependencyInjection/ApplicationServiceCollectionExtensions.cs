using Microsoft.Extensions.DependencyInjection;
using DealTrack.Application.Services;
using DealTrack.Application.ServicesInterfaces;

namespace DealTrack.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IFollowUpService, FollowUpService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}
