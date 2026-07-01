using DealTrack.Domain.Enums;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ICurrentUserService
    {
        string UserId { get; }
        string UserName { get; }
        UserRole Role { get; }
        SubscriptionPlan SubscriptionPlan { get; }
        Guid TenantId { get; }
        bool IsAuthenticated { get; }
    }
}
