using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Targets;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ITargetService
    {
        Task<ApiResponse> SetTargetAsync(CreateTargetDto dto, CancellationToken ct = default);
        Task<ApiResponseT<List<TargetResponseDto>>> GetMyTargetsAsync(int? month, int? year, CancellationToken ct = default);
        Task<ApiResponseT<List<TargetResponseDto>>> GetTeamTargetsAsync(int? month, int? year, CancellationToken ct = default);
        Task<ApiResponse> DeleteTargetAsync(Guid id, CancellationToken ct = default);
    }
}
