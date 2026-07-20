using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.Chat
{
    public class CreateConversationDto
    {
        public bool IsGroup { get; set; }
        public string? Name { get; set; }
        public List<string> ParticipantIds { get; set; } = new();
    }

    public class SendMessageDto
    {
        public Guid ConversationId { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public string? TextContent { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
    }

    public class ConversationDto
    {
        public Guid Id { get; set; }
        public bool IsGroup { get; set; }
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public List<ParticipantDto> Participants { get; set; } = new();
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ParticipantDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public MessageType Type { get; set; }
        public string? TextContent { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class AddGroupMembersDto
    {
        public List<string> UserIds { get; set; } = new();
    }

    public class ChatMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
