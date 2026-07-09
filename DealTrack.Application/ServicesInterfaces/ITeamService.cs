using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Team;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ITeamService
    {
        Task<ApiResponseT<TeamsResponseDto>> GetAllTeamsAsync(CancellationToken ct);
        Task<ApiResponseT<List<TeamMemberDto>>> GetMyTeamAsync(CancellationToken ct);
        Task<ApiResponseT<bool>> RemoveMemberAsync(string userId, CancellationToken ct);
    }
}
