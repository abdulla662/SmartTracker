using DealTrack.API.Hubs;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.SignalR;

namespace DealTrack.API.Services
{
    public class RealtimeNotificationService : IRealtimeNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public RealtimeNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(string userId, object notification, CancellationToken ct = default)
        {
            await _hubContext.Clients
                .Group(userId)
                .SendAsync("ReceiveNotification", notification, ct);
        }
    }
}