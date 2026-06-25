using System.Net;

namespace DealTrack.Application.Common
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? Message { get; set; }
        public IReadOnlyList<string>? Errors { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse SuccessResponse(
            object? data = null,
            string? message = null,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ApiResponse
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static ApiResponse FailureResponse(
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest,
            IReadOnlyList<string>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }
    }
}
