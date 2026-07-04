using DealTrack.Application.Common;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace DealTrack.API.ExceptionMiddleWare
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (InvalidOperationException ex)
            {
                var response = ApiResponse.FailureResponse(ex.Message, HttpStatusCode.BadRequest);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status200OK;
                var json = JsonSerializer.Serialize(response,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await context.Response.WriteAsync(json);
            }
            catch (Exception ex)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
                var method = context.Request.Method;
                var path = context.Request.Path;

                _logger.LogError(ex,
                    "Unhandled exception | {Method} {Path} | User: {UserId} | {Message}",
                    method, path, userId, ex.Message);

                var response = ApiResponse.FailureResponse(
                    "An unexpected error occurred",
                    HttpStatusCode.InternalServerError);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status200OK;

                var json = JsonSerializer.Serialize(response,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                await context.Response.WriteAsync(json);
            }
        }
    }
}
