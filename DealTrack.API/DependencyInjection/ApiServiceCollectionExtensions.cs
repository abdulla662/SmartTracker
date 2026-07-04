using DealTrack.API.Filters;
using DealTrack.API.Services;
using DealTrack.Application.Common;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DealTrack.API.DependencyInjection
{
    public static class ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services)
        {
            services.AddScoped<RequestFilterContext>();
            services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add<ApiResponseStatusCodeFilter>();
            });
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();

                    var response = ApiResponse.FailureResponse(
                        "ValidationError",
                        HttpStatusCode.BadRequest,
                        errors);

                    return new BadRequestObjectResult(response)
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest
                    };
                };
            });
            return services;
        }
    }
}
