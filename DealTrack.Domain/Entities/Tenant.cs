using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public string Name { get; private set; }
        public SubscriptionPlan Plan { get; private set; }

        public ICollection<ApplicationUser> Users { get; private set; } = new List<ApplicationUser>();

        private Tenant() { Name = string.Empty; }

        public Tenant(string name, SubscriptionPlan plan)
        {
            Name = name;
            Plan = plan;
        }

        public void UpdatePlan(SubscriptionPlan plan)
        {
            Plan = plan;
            MarkUpdated();
        }
    }
}
