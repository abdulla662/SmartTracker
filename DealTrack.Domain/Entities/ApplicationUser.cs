
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace DealTrack.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public UserRole Role { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }
        public Guid TenantId { get; set; }
        public Guid? TeamLeadId { get; set; }
        public bool IsApproved { get; set; } = true;

        // Notification preferences
        public bool NotifEmailFollowUps  { get; set; } = true;
        public bool NotifEmailPayments   { get; set; } = true;
        public bool NotifEmailSystem     { get; set; } = false;
        public bool NotifPushFollowUps   { get; set; } = true;
        public bool NotifPushPayments    { get; set; } = true;
        public bool NotifPushOverdue     { get; set; } = true;
        public bool NotifDailyDigest     { get; set; } = false;
        public bool NotifWeeklyReport    { get; set; } = true;
    }
}
