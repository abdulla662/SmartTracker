using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Dashboard;
using DealTrack.Application.DTOs.Landing;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IDashboardService
    {
        Task<ApiResponseT<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default);
        Task<ApiResponseT<LandingStatsDto>> GetLandingStatsAsync(CancellationToken ct = default);
    }
}
