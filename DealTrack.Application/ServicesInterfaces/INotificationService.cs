using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Notifications;
using DealTrack.Domain.Enums;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface INotificationService
    {
        Task CreateAsync(Guid userId, Guid tenantId, string title, string message, NotificationType type, CancellationToken ct = default);
        Task<ApiResponseT<PagedResult<NotificationResponseDto>>> GetMyNotificationsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<ApiResponse> MarkAsReadAsync(Guid id, CancellationToken ct);
        Task<ApiResponse> MarkAllAsReadAsync(CancellationToken ct);
    }
}
