using DealTrack.Application.Common;
using DealTrack.Application.DTOs.ActivityLog;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IActivityLogQueryService
    {
        Task<ApiResponseT<PagedResult<ActivityLogResponseDto>>> GetLogsAsync(ActivityLogFilterDto filter, CancellationToken ct);
        Task<ApiResponseT<List<ActivityLogUserDto>>> GetVisibleUsersAsync(CancellationToken ct);
    }
}
