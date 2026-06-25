using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public string Message { get; private set; }
        public NotificationType Type { get; private set; }
        public bool IsRead { get; private set; }

        private Notification() { Message = string.Empty; }

        public Notification(Guid tenantId, Guid userId, string message, NotificationType type)
        {
            TenantId = tenantId;
            UserId = userId;
            Message = message;
            Type = type;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
            MarkUpdated();
        }
    }
}
