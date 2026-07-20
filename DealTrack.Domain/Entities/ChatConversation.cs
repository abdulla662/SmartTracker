using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities
{
    public class ChatConversation : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public bool IsGroup { get; private set; }
        public string? Name { get; private set; }
        public string? AvatarUrl { get; private set; }
        public string CreatedByUserId { get; private set; }

        public List<ChatParticipant> Participants { get; private set; } = new();
        public List<ChatMessage> Messages { get; private set; } = new();

        private ChatConversation() { CreatedByUserId = string.Empty; }

        public ChatConversation(Guid tenantId, bool isGroup, string createdByUserId, string? name = null)
        {
            TenantId = tenantId;
            IsGroup = isGroup;
            CreatedByUserId = createdByUserId;
            Name = name;
        }

        public void UpdateGroup(string name, string? avatarUrl)
        {
            Name = name;
            AvatarUrl = avatarUrl;
            MarkUpdated();
        }
    }
}
