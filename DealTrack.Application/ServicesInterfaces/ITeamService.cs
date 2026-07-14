using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Team;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ITeamService
    {
        Task<ApiResponseT<TeamsResponseDto>> GetAllTeamsAsync(CancellationToken ct);
        Task<ApiResponseT<List<TeamMemberDto>>> GetMyTeamAsync(CancellationToken ct);
        Task<ApiResponseT<bool>> RemoveMemberAsync(string userId, CancellationToken ct);
        Task<ApiResponseT<MemberProfileDto>> GetMemberProfileAsync(string userId, CancellationToken ct);
        Task<ApiResponseT<bool>> AssignToTeamLeadAsync(string userId, string teamLeadId, CancellationToken ct);
        Task<ApiResponseT<bool>> UnassignFromTeamLeadAsync(string userId, CancellationToken ct);
        Task<ApiResponseT<List<TeamMemberDto>>> GetOtherTeamLeadsAsync(CancellationToken ct);
    }
}
