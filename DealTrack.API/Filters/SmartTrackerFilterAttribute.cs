using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DealTrack.Application.Common;
using DealTrack.Infrastructure.Persistence;
using System.Net;

namespace DealTrack.API.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class DealTrackFilterAttribute : Attribute, IAsyncActionFilter
    {
        public bool Authorize { get; set; }
        public bool ShowDeleted { get; set; }
        public bool IsActive { get; set; } = true;

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var services = context.HttpContext.RequestServices;

            var requestFilterContext =
                services.GetRequiredService<RequestFilterContext>();

            // ======================
            // Authorization
            // ======================
            if (Authorize && !context.HttpContext.User.Identity!.IsAuthenticated)
            {
                context.Result = new JsonResult(
                    ApiResponse.FailureResponse(
                        "Unauthorized",
                        HttpStatusCode.Unauthorized
                    )
                );
                return;
            }

            // ======================
            // Claims
            // ======================
            requestFilterContext.UserId =
                Guid.TryParse(
                    context.HttpContext.User.FindFirst("userId")?.Value,
                    out var uid) ? uid : null;

            requestFilterContext.ShowDeleted = ShowDeleted;
            requestFilterContext.IsActive = IsActive;

            // ======================
            // Apply DB Filters
            // ======================
            var dbContext = services.GetRequiredService<AppDbContext>();
            dbContext.ApplyFilter(requestFilterContext);

            await next();
        }
    }
}
