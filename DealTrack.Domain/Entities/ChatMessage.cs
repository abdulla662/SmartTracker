using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class ChatMessage : BaseEntity
    {
        public Guid ConversationId { get; private set; }
        public Guid TenantId { get; private set; }
        public string SenderId { get; private set; }
        public MessageType Type { get; private set; }
        public string? TextContent { get; private set; }
        public string? FileUrl { get; private set; }
        public string? FileName { get; private set; }
        public long? FileSizeBytes { get; private set; }

        public ChatConversation? Conversation { get; private set; }

        private ChatMessage() { SenderId = string.Empty; }

        public ChatMessage(Guid conversationId, Guid tenantId, string senderId,
            MessageType type, string? textContent = null,
            string? fileUrl = null, string? fileName = null, long? fileSizeBytes = null)
        {
            ConversationId = conversationId;
            TenantId = tenantId;
            SenderId = senderId;
            Type = type;
            TextContent = textContent;
            FileUrl = fileUrl;
            FileName = fileName;
            FileSizeBytes = fileSizeBytes;
        }
    }
}
