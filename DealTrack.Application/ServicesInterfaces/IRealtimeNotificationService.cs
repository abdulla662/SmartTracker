namespace DealTrack.Application.ServicesInterfaces
{
    public interface IRealtimeNotificationService
    {
        Task SendNotificationAsync(string userId, object notification, CancellationToken ct = default);
    }
}