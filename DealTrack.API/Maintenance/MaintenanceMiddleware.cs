using System.Text.Json;

namespace DealTrack.API.Maintenance
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            // Allow kill switch and health ping through during maintenance
            if (context.Request.Path.StartsWithSegments("/api/ping") ||
                context.Request.Path.StartsWithSegments("/api/m3r9-ctrl"))
            {
                await _next(context);
                return;
            }

            if (MaintenanceState.IsActive)
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    maintenance = true,
                    until = MaintenanceState.Until?.ToString("o"),
                    message = "System is under maintenance."
                }));
                return;
            }

            await _next(context);
        }
    }
}
