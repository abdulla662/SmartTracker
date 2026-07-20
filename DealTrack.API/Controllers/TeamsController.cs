using DealTrack.Application.Common;
using DealTrack.Application.DTOs.JoinRequest;
using DealTrack.Application.DTOs.Team;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IJoinRequestService _joinRequestService;

        public TeamsController(ITeamService teamService, IJoinRequestService joinRequestService)
        {
            _teamService = teamService;
            _joinRequestService = joinRequestService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ApiResponseT<TeamsResponseDto>> GetAllTeams(CancellationToken ct)
            => await _teamService.GetAllTeamsAsync(ct);

        [HttpGet("my-team")]
        [Authorize(Roles = "TeamLead")]
        public async Task<ApiResponseT<List<TeamMemberDto>>> GetMyTeam(CancellationToken ct)
            => await _teamService.GetMyTeamAsync(ct);

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin,TeamLead")]
        public async Task<ApiResponseT<bool>> RemoveMember(string userId, CancellationToken ct)
            => await _teamService.RemoveMemberAsync(userId, ct);

        // Member profile
        [HttpGet("members/{userId}/profile")]
        [Authorize(Roles = "Admin,TeamLead")]
        public async Task<ApiResponseT<MemberProfileDto>> GetMemberProfile(string userId, CancellationToken ct)
            => await _teamService.GetMemberProfileAsync(userId, ct);

        [HttpPut("members/{userId}/assign-teamlead/{teamLeadId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResponseT<bool>> AssignToTeamLead(string userId, string teamLeadId, CancellationToken ct)
            => await _teamService.AssignToTeamLeadAsync(userId, teamLeadId, ct);

        [HttpPut("members/{userId}/unassign-teamlead")]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResponseT<bool>> UnassignFromTeamLead(string userId, CancellationToken ct)
            => await _teamService.UnassignFromTeamLeadAsync(userId, ct);

        [HttpGet("other-leads")]
        [Authorize(Roles = "TeamLead")]
        public async Task<ApiResponseT<List<TeamMemberDto>>> GetOtherTeamLeads(CancellationToken ct)
            => await _teamService.GetOtherTeamLeadsAsync(ct);

        // Join Requests
        [HttpGet("join-requests")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ApiResponseT<List<JoinRequestDto>>> GetPendingJoinRequests(CancellationToken ct)
            => await _joinRequestService.GetPendingAsync(ct);

        [HttpPost("join-requests/{userId}/accept")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ApiResponse> AcceptJoinRequest(string userId, CancellationToken ct)
            => await _joinRequestService.AcceptAsync(userId, ct);

        [HttpPost("join-requests/{userId}/reject")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ApiResponse> RejectJoinRequest(string userId, CancellationToken ct)
            => await _joinRequestService.RejectAsync(userId, ct);
    }
}
