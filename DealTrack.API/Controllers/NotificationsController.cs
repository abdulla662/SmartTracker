using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Notifications;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ApiResponseT<PagedResult<NotificationResponseDto>>> GetMyNotifications(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
            => await _notificationService.GetMyNotificationsAsync(page, pageSize, ct);

        [HttpPut("{id:guid}/read")]
        public async Task<ApiResponse> MarkAsRead(Guid id, CancellationToken ct)
            => await _notificationService.MarkAsReadAsync(id, ct);

        [HttpPut("read-all")]
        public async Task<ApiResponse> MarkAllAsRead(CancellationToken ct)
            => await _notificationService.MarkAllAsReadAsync(ct);
    }
}
