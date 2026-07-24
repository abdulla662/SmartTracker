using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public string Name { get; private set; }
        public SubscriptionPlan Plan { get; private set; }
        public bool IsPersonal { get; private set; }
        public string? Industry { get; private set; }
        public string? Address { get; private set; }
        public string? Website { get; private set; }
        public string Currency { get; private set; } = "EGP";
        public string Timezone { get; private set; } = "Africa/Cairo";

        public ICollection<ApplicationUser> Users { get; private set; } = new List<ApplicationUser>();
        public ICollection<TenantInvite> TenantInvites { get; private set; } = new List<TenantInvite>();

        private Tenant() { Name = string.Empty; }

        public Tenant(string name, SubscriptionPlan plan, bool isPersonal = false)
        {
            Name = name;
            Plan = plan;
            IsPersonal = isPersonal;
        }

        public DateTime? PlanExpiresAt { get; private set; }

        public void UpdatePlan(SubscriptionPlan plan, DateTime? expiresAt = null)
        {
            Plan = plan;
            PlanExpiresAt = expiresAt;
            MarkUpdated();
        }

        public bool IsBlocked { get; private set; }
        public DateTime? BlockedUntil { get; private set; }
        public string? BlockReason { get; private set; }

        public void Block(DateTime? until, string? reason)
        {
            IsBlocked = true;
            BlockedUntil = until;
            BlockReason = reason;
            MarkUpdated();
        }

        public void Unblock()
        {
            IsBlocked = false;
            BlockedUntil = null;
            BlockReason = null;
            MarkUpdated();
        }

        public void UpdateInfo(string name, string? industry, string? address, string? website, string? currency, string? timezone)
        {
            Name = name;
            Industry = industry;
            Address = address;
            Website = website;
            Currency = string.IsNullOrWhiteSpace(currency) ? "EGP" : currency;
            Timezone = string.IsNullOrWhiteSpace(timezone) ? "Africa/Cairo" : timezone;
            MarkUpdated();
        }
    }
}
