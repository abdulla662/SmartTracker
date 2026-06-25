using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities
{
    public class ActivityLog : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public string Action { get; private set; }
        public Guid? EntityId { get; private set; }

        private ActivityLog() { Action = string.Empty; }

        public ActivityLog(Guid tenantId, Guid userId, string action, Guid? entityId = null)
        {
            TenantId = tenantId;
            UserId = userId;
            Action = action;
            EntityId = entityId;
        }
    }
}
