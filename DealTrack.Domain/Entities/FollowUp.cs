using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class FollowUp : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public Guid ClientId { get; private set; }
        public Client Client { get; private set; }

        public DateTime FollowUpDate { get; private set; }
        public FollowUpStatus Status { get; private set; }
        public string? Notes { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        private FollowUp() { }

        public FollowUp(Guid tenantId, Guid clientId, DateTime followUpDate, Guid createdByUserId, string? notes = null)
        {
            TenantId = tenantId;
            ClientId = clientId;
            FollowUpDate = followUpDate;
            CreatedByUserId = createdByUserId;
            Status = FollowUpStatus.Pending;
            Notes = notes;
        }

        public void MarkDone()
        {
            Status = FollowUpStatus.Done;
            MarkUpdated();
        }

        public void MarkMissed()
        {
            Status = FollowUpStatus.Missed;
            MarkUpdated();
        }

        public void UpdateNotes(string? notes)
        {
            Notes = notes;
            MarkUpdated();
        }

        public void UpdateDate(DateTime date)
        {
            FollowUpDate = date;
            MarkUpdated();
        }

        public void ChangeStatus(FollowUpStatus status)
        {
            Status = status;
            MarkUpdated();
        }
    }
}
