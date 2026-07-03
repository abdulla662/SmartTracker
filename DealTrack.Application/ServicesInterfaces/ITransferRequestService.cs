using DealTrack.Application.Common;
using DealTrack.Application.DTOs.TransferRequest;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ITransferRequestService
    {
        Task<ApiResponseT<TransferRequestResponseDto>> CreateTransferRequestAsync(CreateTransferRequestDto dto, CancellationToken ct);
        Task<ApiResponse> RespondToTransferRequestAsync(Guid requestId, RespondTransferRequestDto dto, CancellationToken ct);
        Task<ApiResponseT<List<TransferRequestResponseDto>>> GetMyTransferRequestsAsync(CancellationToken ct);
    }
}