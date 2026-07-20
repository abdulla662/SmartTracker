using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Notifications;
using DealTrack.Application.Helpers;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.Extensions.Localization;
using System.Net;
using System.Text.Json;

namespace DealTrack.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IRealtimeNotificationService _realtimeService;

        public NotificationService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, IRealtimeNotificationService realtimeService)
        {
            _uow = uow;
            _currentUser = currentUser;
            _realtimeService = realtimeService;
            _localizer = localizer;
        }

        public async Task CreateAsync(Guid userId, Guid tenantId, string title, string message, NotificationType type, CancellationToken ct = default)
        {
            var notification = new Notification(tenantId, userId, title, message, type);
            await _uow.Write<Notification>().AddAsync(notification, ct);
            await _uow.SaveChangesAsync();
            await _realtimeService.SendNotificationAsync(userId.ToString(), new
            {
                id = notification.Id,
                title = LocalizeNotif(notification.Title),
                message = LocalizeNotif(notification.Message),
                link = ExtractLink(notification.Message),
                type = notification.Type,
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt
            }, ct);
        }

        public async Task NotifyUpstreamAsync(string actorUserId, Guid tenantId, string actionKey, string[] actionParams, string? link = null, CancellationToken ct = default)
        {
            var actor = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == actorUserId, ct);
            if (actor == null) return;

            var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.UserName ?? "" : actor.FullName;

            if (actor.Role == UserRole.Sales)
            {
                if (actor.TeamLeadId.HasValue)
                {
                    var tlId = actor.TeamLeadId.Value.ToString();
                    var teamLead = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == tlId, ct);
                    if (teamLead != null)
                    {
                        var tlName = string.IsNullOrWhiteSpace(teamLead.FullName) ? teamLead.UserName ?? "" : teamLead.FullName;
                        var msgParams = new[] { actorName, tlName }.Concat(actionParams).ToArray();
                        await CreateAsync(actor.TeamLeadId.Value, tenantId,
                            NotifKey.Build("notif.title.teamActivity"),
                            NotifKey.Build($"notif.msg.salesLed.{actionKey}", msgParams, link),
                            NotificationType.SystemNotification, ct);
                    }
                }
                else
                {
                    var admins = await _uow.Read<ApplicationUser>()
                        .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.Admin, ct);
                    var msgParams = new[] { actorName }.Concat(actionParams).ToArray();
                    foreach (var admin in admins)
                        await CreateAsync(Guid.Parse(admin.Id), tenantId,
                            NotifKey.Build("notif.title.teamActivity"),
                            NotifKey.Build($"notif.msg.sales.{actionKey}", msgParams, link),
                            NotificationType.SystemNotification, ct);
                }
            }
            else if (actor.Role == UserRole.TeamLead)
            {
                var admins = await _uow.Read<ApplicationUser>()
                    .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.Admin, ct);
                var msgParams = new[] { actorName }.Concat(actionParams).ToArray();
                foreach (var admin in admins)
                    await CreateAsync(Guid.Parse(admin.Id), tenantId,
                        NotifKey.Build("notif.title.tlActivity"),
                        NotifKey.Build($"notif.msg.tl.{actionKey}", msgParams, link),
                        NotificationType.SystemNotification, ct);
            }
        }

        public async Task<ApiResponseT<PagedResult<NotificationResponseDto>>> GetMyNotificationsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var userId = Guid.Parse(_currentUser.UserId);

            var notifications = await _uow.Read<Notification>()
                .ListAsync(n => n.UserId == userId, ct);

            var ordered = notifications.OrderByDescending(n => n.CreatedAt).ToList();
            var totalCount = ordered.Count;
            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = LocalizeNotif(n.Title),
                    Message = LocalizeNotif(n.Message),
                    Link = ExtractLink(n.Message),
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

            return ApiResponseT<PagedResult<NotificationResponseDto>>.SuccessResponse(
                new PagedResult<NotificationResponseDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
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

        private string LocalizeNotif(string raw)
        {
            if (string.IsNullOrEmpty(raw) || !raw.TrimStart().StartsWith("{")) return raw;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (!doc.RootElement.TryGetProperty("k", out var kProp)) return raw;
                var key = kProp.GetString() ?? "";
                var template = _localizer[key].Value;
                if (doc.RootElement.TryGetProperty("p", out var pProp))
                {
                    var args = pProp.EnumerateArray().Select(e => (object)(e.GetString() ?? "")).ToArray();
                    return args.Length > 0 ? string.Format(template, args) : template;
                }
                return template;
            }
            catch { return raw; }
        }

        private static string? ExtractLink(string raw)
        {
            if (string.IsNullOrEmpty(raw) || !raw.TrimStart().StartsWith("{")) return null;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("l", out var lProp))
                    return lProp.GetString();
            }
            catch { /* ignore */ }
            return null;
        }
    }
}
