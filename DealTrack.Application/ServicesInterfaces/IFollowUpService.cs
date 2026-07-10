using DealTrack.Application.Common;
using DealTrack.Application.DTOs.FollowUps;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IFollowUpService
    {
        Task<ApiResponseT<PagedResult<FollowUpResponseDto>>> GetAllFollowUpsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<ApiResponseT<PagedResult<FollowUpResponseDto>>> GetFollowUpsForClientAsync(Guid clientId, int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<ApiResponseT<FollowUpResponseDto>> CreateFollowUpAsync(CreateFollowUpDto dto, CancellationToken ct = default);
        Task<ApiResponse> MarkDoneAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponse> MarkMissedAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponse> DeleteFollowUpAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponse> UpdateFollowUpAsync(Guid id, UpdateFollowUpDto dto, CancellationToken ct);
    }
}
