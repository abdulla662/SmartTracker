using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Profile;
using Microsoft.AspNetCore.Http;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IProfileService
    {
        public Task<ApiResponseT<GetProfileDto>> GetProfileAsync(CancellationToken ct);
        public Task<ApiResponseT<GetProfileDto>> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct);
        public Task<ApiResponse> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct);
        public Task<ApiResponseT<NotificationPrefsDto>> GetNotificationPrefsAsync(CancellationToken ct);
        public Task<ApiResponse> UpdateNotificationPrefsAsync(NotificationPrefsDto dto, CancellationToken ct);
        public Task<ApiResponseT<string>> UploadProfileImageAsync(IFormFile file, string webRootPath, CancellationToken ct);
    }
}
