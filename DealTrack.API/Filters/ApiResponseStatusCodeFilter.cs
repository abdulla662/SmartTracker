using DealTrack.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DealTrack.API.Filters
{
    public class ApiResponseStatusCodeFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult objectResult &&
                objectResult.Value is ApiResponse apiResponse)
            {
                objectResult.StatusCode = (int)apiResponse.StatusCode;
            }
        }
    }
}
