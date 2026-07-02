using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Team;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResponseT<TeamsResponseDto>> GetAllTeams(CancellationToken ct)
            => await _teamService.GetAllTeamsAsync(ct);

        [HttpGet("my-team")]
        [Authorize(Roles = "TeamLead")]
        public async Task<ApiResponseT<List<TeamMemberDto>>> GetMyTeam(CancellationToken ct)
            => await _teamService.GetMyTeamAsync(ct);
    }
}
