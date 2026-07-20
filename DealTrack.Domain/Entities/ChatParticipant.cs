namespace DealTrack.Domain.Entities
{
    public class ChatParticipant
    {
        public Guid ConversationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }

        public ChatConversation? Conversation { get; set; }
    }
}
