using AutoMapper;
using DealTrack.Application.Mappings;
using DealTrack.Application.Services;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.Extensions.DependencyInjection;

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
            // IDashboardService is registered in InfrastructureServiceCollectionExtensions
            services.AddScoped<IInviteService, InviteService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<ITransferRequestService, TransferRequestService>();
            services.AddScoped<IActivityLogService, ActivityLogService>();
            services.AddScoped<IActivityLogQueryService, ActivityLogQueryService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IJoinRequestService, JoinRequestService>();
            services.AddScoped<ITargetService, TargetService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<ISalaryService, SalaryService>();
            services.AddScoped<IHRService, HRService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddAutoMapper(typeof(MappingProfile).Assembly);

            return services;
        }
    }
}
