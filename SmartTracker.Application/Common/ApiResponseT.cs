using System.Net;

namespace SmartTracker.Application.Common
{
    public class ApiResponseT<T> : ApiResponse
    {
        public new T? Data
        {
            get => (T?)base.Data;
            set => base.Data = value;
        }

        public static ApiResponseT<T> SuccessResponse(
            T data,
            string? message = null,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ApiResponseT<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static ApiResponseT<T> FailureResponse(
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest,
            IReadOnlyList<string>? errors = null)
        {
            return new ApiResponseT<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }
    }
}
