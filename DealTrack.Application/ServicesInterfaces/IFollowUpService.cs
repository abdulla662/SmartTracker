using DealTrack.Application.Common;
using DealTrack.Application.DTOs.FollowUps;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IFollowUpService
    {
        Task<ApiResponseT<List<FollowUpResponseDto>>> GetFollowUpsForClientAsync(Guid clientId, CancellationToken ct = default);
        Task<ApiResponseT<FollowUpResponseDto>> CreateFollowUpAsync(CreateFollowUpDto dto, CancellationToken ct = default);
        Task<ApiResponse> MarkDoneAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponse> MarkMissedAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponse> DeleteFollowUpAsync(Guid id, CancellationToken ct = default);
    }
}
