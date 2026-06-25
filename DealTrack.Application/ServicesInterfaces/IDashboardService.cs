using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Dashboard;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IDashboardService
    {
        Task<ApiResponseT<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default);
    }
}
