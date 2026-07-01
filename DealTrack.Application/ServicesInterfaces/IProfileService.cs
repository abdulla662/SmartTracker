using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Profile;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IProfileService
    {
        public Task<ApiResponseT<GetProfileDto>> GetProfileAsync(CancellationToken ct);
        public Task<ApiResponseT<GetProfileDto>> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct);
        public Task<ApiResponse> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct);
    }
}
