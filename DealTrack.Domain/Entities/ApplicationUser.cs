
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace DealTrack.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }
        public Guid TenantId { get; set; }
    }
}
