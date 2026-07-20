using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class HRActionRequest : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public string RequestedByUserId { get; private set; }
        public string TargetUserId { get; private set; }
        public string? TargetTeamLeadId { get; private set; }
        public HRActionType ActionType { get; private set; }
        public string Description { get; private set; }
        public HRActionStatus Status { get; private set; }
        public string? ReviewedByAdminId { get; private set; }
        public string? AdminNote { get; private set; }
        public DateTime? ReviewedAt { get; private set; }
        public decimal? Amount { get; private set; }
        public Guid? JoinRequestId { get; private set; }

        private HRActionRequest() { RequestedByUserId = string.Empty; TargetUserId = string.Empty; Description = string.Empty; }

        public HRActionRequest(Guid tenantId, string requestedByUserId, string targetUserId, HRActionType actionType, string description, string? targetTeamLeadId = null, decimal? amount = null, Guid? joinRequestId = null)
        {
            TenantId = tenantId;
            RequestedByUserId = requestedByUserId;
            TargetUserId = targetUserId;
            TargetTeamLeadId = targetTeamLeadId;
            ActionType = actionType;
            Description = description;
            Status = HRActionStatus.Pending;
            Amount = amount;
            JoinRequestId = joinRequestId;
        }

        public void Approve(string adminId, string? note)
        {
            Status = HRActionStatus.Approved;
            ReviewedByAdminId = adminId;
            AdminNote = note;
            ReviewedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        public void Reject(string adminId, string? note)
        {
            Status = HRActionStatus.Rejected;
            ReviewedByAdminId = adminId;
            AdminNote = note;
            ReviewedAt = DateTime.UtcNow;
            MarkUpdated();
        }
    }
}
