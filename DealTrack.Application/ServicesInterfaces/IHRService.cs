using DealTrack.Application.Common;
using DealTrack.Application.DTOs.HR;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IHRService
    {
        Task<ApiResponse> CreateActionRequestAsync(CreateHRActionDto dto, CancellationToken ct = default);
        Task<ApiResponse> ReviewActionRequestAsync(Guid id, ReviewHRActionDto dto, CancellationToken ct = default);
        Task<ApiResponseT<List<HRActionResponseDto>>> GetActionRequestsAsync(CancellationToken ct = default);
        Task<ApiResponse> DeleteActionAsync(Guid id, CancellationToken ct = default);
    }
}
