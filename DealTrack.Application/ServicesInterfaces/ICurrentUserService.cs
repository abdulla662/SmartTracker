namespace DealTrack.Application.ServicesInterfaces
{
    public interface ICurrentUserService
    {
        string UserId { get; }
        string UserName { get; }
        string Role { get; }
        string SubscriptionPlan { get; }
        Guid TenantId { get; }
        bool IsAuthenticated { get; }
    }
}
