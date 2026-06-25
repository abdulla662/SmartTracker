using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities
{
    public class Client : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public string Name { get; private set; }
        public string Phone { get; private set; }
        public string? Notes { get; private set; }

        public string AssignedToUserId { get; private set; }
        public ApplicationUser AssignedToUser { get; private set; }

        public ICollection<FollowUp> FollowUps { get; private set; } = new List<FollowUp>();
        public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

        private Client() { Name = string.Empty; Phone = string.Empty; AssignedToUserId = string.Empty; }

        public Client(Guid tenantId, string name, string phone, string? notes, string assignedToUserId)
        {
            TenantId = tenantId;
            Name = name;
            Phone = phone;
            Notes = notes;
            AssignedToUserId = assignedToUserId;
        }

        public void Update(string name, string phone, string? notes)
        {
            Name = name;
            Phone = phone;
            Notes = notes;
            MarkUpdated();
        }
    }
}
