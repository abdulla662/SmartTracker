using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Profile;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using Microsoft.AspNetCore.Http;
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
            user.Description = dto.Description ?? string.Empty;
            user.Email = dto.Email;
            user.UserName = dto.Email;
            if (dto.Phone != null) user.PhoneNumber = dto.Phone;

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

        public async Task<ApiResponseT<NotificationPrefsDto>> GetNotificationPrefsAsync(CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.UserId);
            if (user == null)
                return ApiResponseT<NotificationPrefsDto>.FailureResponse(_localizer["UserNotFound"]);

            return ApiResponseT<NotificationPrefsDto>.SuccessResponse(new NotificationPrefsDto
            {
                EmailFollowUps = user.NotifEmailFollowUps,
                EmailPayments  = user.NotifEmailPayments,
                EmailSystem    = user.NotifEmailSystem,
                PushFollowUps  = user.NotifPushFollowUps,
                PushPayments   = user.NotifPushPayments,
                PushOverdue    = user.NotifPushOverdue,
                DailyDigest    = user.NotifDailyDigest,
                WeeklyReport   = user.NotifWeeklyReport,
            });
        }

        public async Task<ApiResponse> UpdateNotificationPrefsAsync(NotificationPrefsDto dto, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.UserId);
            if (user == null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"]);

            user.NotifEmailFollowUps = dto.EmailFollowUps;
            user.NotifEmailPayments  = dto.EmailPayments;
            user.NotifEmailSystem    = dto.EmailSystem;
            user.NotifPushFollowUps  = dto.PushFollowUps;
            user.NotifPushPayments   = dto.PushPayments;
            user.NotifPushOverdue    = dto.PushOverdue;
            user.NotifDailyDigest    = dto.DailyDigest;
            user.NotifWeeklyReport   = dto.WeeklyReport;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ApiResponse.FailureResponse(result.Errors.First().Description);

            return ApiResponse.SuccessResponse(message: _localizer["PreferencesSaved"]);
        }

        public async Task<ApiResponseT<string>> UploadProfileImageAsync(IFormFile file, string webRootPath, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.UserId);
            if (user == null)
                return ApiResponseT<string>.FailureResponse(_localizer["UserNotFound"]);

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return ApiResponseT<string>.FailureResponse("Only JPG, PNG, or WebP images are allowed.");

            if (file.Length > 5 * 1024 * 1024)
                return ApiResponseT<string>.FailureResponse("Image must be smaller than 5 MB.");

            var folder = Path.Combine(webRootPath, "uploads", "profiles");
            Directory.CreateDirectory(folder);

            // Delete old image if it exists
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var oldPath = Path.Combine(webRootPath, user.ProfileImageUrl.TrimStart('/'));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var fileName = $"{user.Id}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream, ct);

            var url = $"/uploads/profiles/{fileName}";
            user.ProfileImageUrl = url;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ApiResponseT<string>.FailureResponse(result.Errors.First().Description);

            return ApiResponseT<string>.SuccessResponse(url, "Profile image updated.");
        }
    }
}
