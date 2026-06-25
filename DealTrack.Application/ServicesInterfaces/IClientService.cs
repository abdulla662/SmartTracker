using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Clients;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IClientService
    {
        Task<ApiResponseT<PagedResult<ClientResponseDto>>> GetClientsAsync(ClientFilterDto filter, CancellationToken ct = default);
        Task<ApiResponseT<ClientResponseDto>> GetClientByIdAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponseT<ClientResponseDto>> CreateClientAsync(CreateClientDto dto, CancellationToken ct = default);
        Task<ApiResponse> UpdateClientAsync(Guid id, UpdateClientDto dto, CancellationToken ct = default);
        Task<ApiResponse> DeleteClientAsync(Guid id, CancellationToken ct = default);
    }
}
