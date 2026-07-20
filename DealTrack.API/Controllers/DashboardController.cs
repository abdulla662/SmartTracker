using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Dashboard;
using DealTrack.Application.DTOs.Landing;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<ApiResponseT<DashboardSummaryDto>> GetSummary(CancellationToken ct)
        {
            return await _dashboardService.GetSummaryAsync(ct);
        }

        [HttpGet("landing-stats")]
        [AllowAnonymous]
        public async Task<ApiResponseT<LandingStatsDto>> GetLandingStats(CancellationToken ct)
        {
            return await _dashboardService.GetLandingStatsAsync(ct);
        }
    }
}
