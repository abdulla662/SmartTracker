using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Notifications;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public NotificationService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task CreateAsync(Guid userId, Guid tenantId, string title, string message, NotificationType type, CancellationToken ct = default)
        {
            var notification = new Notification(tenantId, userId, title, message, type);
            await _uow.Write<Notification>().AddAsync(notification, ct);
            await _uow.SaveChangesAsync();
        }

        public async Task<ApiResponseT<List<NotificationResponseDto>>> GetMyNotificationsAsync(CancellationToken ct)
        {
            var userId = Guid.Parse(_currentUser.UserId);

            var notifications = await _uow.Read<Notification>()
                .ListAsync(n => n.UserId == userId, ct);

            var dtos = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

            return ApiResponseT<List<NotificationResponseDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponse> MarkAsReadAsync(Guid id, CancellationToken ct)
        {
            var notification = await _uow.Read<Notification>().GetByIdAsync(id, ct);
            if (notification is null)
                return ApiResponse.FailureResponse(_localizer["NotificationNotFound"], HttpStatusCode.NotFound);

            if (notification.UserId != Guid.Parse(_currentUser.UserId))
                return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            notification.MarkAsRead();
            await _uow.Write<Notification>().UpdateAsync(notification, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["NotificationMarkedRead"]);
        }

        public async Task<ApiResponse> MarkAllAsReadAsync(CancellationToken ct)
        {
            var userId = Guid.Parse(_currentUser.UserId);

            var notifications = await _uow.Read<Notification>()
                .ListAsync(n => n.UserId == userId && !n.IsRead, ct);

            foreach (var notification in notifications)
            {
                notification.MarkAsRead();
                await _uow.Write<Notification>().UpdateAsync(notification, ct);
            }

            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["AllNotificationsMarkedRead"]);
        }
    }
}