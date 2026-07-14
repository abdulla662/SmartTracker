using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Profile;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IWebHostEnvironment _env;

        public ProfileController(IProfileService profileService, IWebHostEnvironment env)
        {
            _profileService = profileService;
            _env = env;
        }

        [HttpGet]
        public async Task<ApiResponseT<GetProfileDto>> GetProfile(CancellationToken ct)
            => await _profileService.GetProfileAsync(ct);

        [HttpPut]
        public async Task<ApiResponseT<GetProfileDto>> UpdateProfile(
            [FromBody] UpdateProfileDto dto,
            CancellationToken ct)
            => await _profileService.UpdateProfileAsync(dto, ct);

        [HttpPut("change-password")]
        public async Task<ApiResponse> ChangePassword(
            [FromBody] ChangePasswordDto dto,
            CancellationToken ct)
            => await _profileService.ChangePasswordAsync(dto, ct);

        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<ApiResponseT<string>> UploadProfileImage(
            IFormFile file,
            CancellationToken ct)
            => await _profileService.UploadProfileImageAsync(file, _env.WebRootPath, ct);

        [HttpGet("notification-prefs")]
        public async Task<ApiResponseT<NotificationPrefsDto>> GetNotificationPrefs(CancellationToken ct)
            => await _profileService.GetNotificationPrefsAsync(ct);

        [HttpPut("notification-prefs")]
        public async Task<ApiResponse> UpdateNotificationPrefs(
            [FromBody] NotificationPrefsDto dto,
            CancellationToken ct)
            => await _profileService.UpdateNotificationPrefsAsync(dto, ct);
    }
}
