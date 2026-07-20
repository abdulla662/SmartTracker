using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Tenant;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ITenantService
    {
        Task<ApiResponseT<TenantInfoDto>> GetTenantInfoAsync(CancellationToken ct = default);
        Task<ApiResponse> UpdateTenantAsync(UpdateTenantDto dto, CancellationToken ct = default);
    }
}
