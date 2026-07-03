using DealTrack.Application.Common;
using DealTrack.Application.DTOs.ActivityLog;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityLogQueryService _activityLogQueryService;

        public ActivityLogsController(IActivityLogQueryService activityLogQueryService)
        {
            _activityLogQueryService = activityLogQueryService;
        }

        [HttpGet]
        public async Task<ApiResponseT<PagedResult<ActivityLogResponseDto>>> GetLogs(
            [FromQuery] ActivityLogFilterDto filter, CancellationToken ct)
            => await _activityLogQueryService.GetLogsAsync(filter, ct);
    }
}