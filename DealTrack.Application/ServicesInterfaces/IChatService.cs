using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Chat;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IChatService
    {
        Task<ApiResponseT<List<ChatMemberDto>>> GetTenantMembersAsync(CancellationToken ct = default);
        Task<List<string>> GetParticipantIdsAsync(Guid conversationId, CancellationToken ct = default);
        Task<ApiResponseT<ConversationDto>> GetOrCreateDMAsync(string otherUserId, CancellationToken ct = default);
        Task<ApiResponseT<ConversationDto>> CreateGroupAsync(CreateConversationDto dto, CancellationToken ct = default);
        Task<ApiResponseT<List<ConversationDto>>> GetMyConversationsAsync(CancellationToken ct = default);
        Task<ApiResponseT<List<MessageDto>>> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken ct = default);
        Task<ApiResponseT<MessageDto>> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default);
        Task<ApiResponseT<MessageDto>> SendMessageFromHubAsync(SendMessageDto dto, string senderId, Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse> MarkReadAsync(Guid conversationId, CancellationToken ct = default);
        Task<ApiResponse> AddMembersAsync(Guid conversationId, AddGroupMembersDto dto, CancellationToken ct = default);
        Task<ApiResponse> LeaveGroupAsync(Guid conversationId, CancellationToken ct = default);
    }
}
