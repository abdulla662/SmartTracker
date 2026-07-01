using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Profile;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace DealTrack.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public ProfileService(
            IUnitOfWork uow,
            ICurrentUserService currentUser,
            IStringLocalizer<SharedResource> localizer,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<ApiResponseT<GetProfileDto>> GetProfileAsync(CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.UserId);
            if (user == null)
                return ApiResponseT<GetProfileDto>.FailureResponse(_localizer["UserNotFound"]);

            return ApiResponseT<GetProfileDto>.SuccessResponse(_mapper.Map<GetProfileDto>(user));
        }

        public async Task<ApiResponseT<GetProfileDto>> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.UserId);
            if (user == null)
                return ApiResponseT<GetProfileDto>.FailureResponse(_localizer["UserNotFound"]);

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.UserName = dto.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ApiResponseT<GetProfileDto>.FailureResponse(result.Errors.First().Description);

            return ApiResponseT<GetProfileDto>.SuccessResponse(_mapper.Map<GetProfileDto>(user), _localizer["ProfileUpdated"]);
        }

        public async Task<ApiResponse> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.UserId);
            if (user == null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"]);

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return ApiResponse.FailureResponse(result.Errors.First().Description);

            return ApiResponse.SuccessResponse(message: _localizer["PasswordChanged"]);
        }
    }
}
