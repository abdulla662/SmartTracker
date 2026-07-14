using DealTrack.Application.Common;
using DealTrack.Application.DTOs.JoinRequest;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IJoinRequestService
    {
        Task<ApiResponseT<List<JoinRequestDto>>> GetPendingAsync(CancellationToken ct = default);
        Task<ApiResponse> AcceptAsync(string userId, CancellationToken ct = default);
        Task<ApiResponse> RejectAsync(string userId, CancellationToken ct = default);
    }
}
