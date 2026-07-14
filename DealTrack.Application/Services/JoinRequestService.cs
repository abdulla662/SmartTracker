using DealTrack.Application.Common;
using DealTrack.Application.DTOs.JoinRequest;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class JoinRequestService : IJoinRequestService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notifications;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public JoinRequestService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow,
            ICurrentUserService currentUser,
            INotificationService notifications,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _uow = uow;
            _currentUser = currentUser;
            _notifications = notifications;
            _localizer = localizer;
        }

        public Task<ApiResponseT<List<JoinRequestDto>>> GetPendingAsync(CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin)
                return Task.FromResult(ApiResponseT<List<JoinRequestDto>>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden));

            var tenantId = _currentUser.TenantId;

            var result = _userManager.Users
                .Where(u => u.TenantId == tenantId && !u.IsApproved)
                .Select(u => new JoinRequestDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    Role = u.Role.ToString(),
                    RequestedAt = DateTime.UtcNow
                }).ToList();

            return Task.FromResult(ApiResponseT<List<JoinRequestDto>>.SuccessResponse(result, _localizer["JoinRequestsRetrieved"]));
        }

        public async Task<ApiResponse> AcceptAsync(string userId, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantId != _currentUser.TenantId || user.IsApproved)
                return ApiResponse.FailureResponse(_localizer["JoinRequestNotFound"], HttpStatusCode.NotFound);

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            // Notify the accepted user
            await _notifications.CreateAsync(
                Guid.Parse(user.Id),
                user.TenantId,
                "Join Request Approved",
                $"Your request to join the company has been approved. Welcome to the team!",
                NotificationType.SystemNotification,
                ct);

            return ApiResponse.SuccessResponse(message: _localizer["JoinRequestAccepted"]);
        }

        public async Task<ApiResponse> RejectAsync(string userId, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantId != _currentUser.TenantId || user.IsApproved)
                return ApiResponse.FailureResponse(_localizer["JoinRequestNotFound"], HttpStatusCode.NotFound);

            await _userManager.DeleteAsync(user);

            return ApiResponse.SuccessResponse(message: _localizer["JoinRequestRejected"]);
        }
    }
}
