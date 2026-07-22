using DealTrack.Application.Common;
using DealTrack.Application.DTOs.JoinRequest;
using DealTrack.Application.DTOs.Team;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUser;

        public TeamsController(ITeamService teamService, IJoinRequestService joinRequestService,
            UserManager<ApplicationUser> userManager, ICurrentUserService currentUser)
        {
            _teamService = teamService;
            _joinRequestService = joinRequestService;
            _userManager = userManager;
            _currentUser = currentUser;
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

        // ── Block a member (Admin only) ──────────────────────────────────────
        [HttpPost("members/{userId}/block")]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResponse> BlockMember(string userId, CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantId != tenantId || user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                return ApiResponse.FailureResponse("User not found or not allowed.", System.Net.HttpStatusCode.NotFound);

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);
            return ApiResponse.SuccessResponse(message: "Member blocked.");
        }

        // ── Unblock a member (Admin only) ────────────────────────────────────
        [HttpPost("members/{userId}/unblock")]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResponse> UnblockMember(string userId, CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantId != tenantId || user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                return ApiResponse.FailureResponse("User not found or not allowed.", System.Net.HttpStatusCode.NotFound);

            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);
            return ApiResponse.SuccessResponse(message: "Member unblocked.");
        }
    }
}
