using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Profile;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
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
    }
}
