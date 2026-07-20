using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Chat;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using System.Net;

namespace DealTrack.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public ChatService(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<List<string>> GetParticipantIdsAsync(Guid conversationId, CancellationToken ct = default)
        {
            var participants = await _uow.Read<ChatParticipant>()
                .ListAsync(p => p.ConversationId == conversationId, ct);
            return participants.Select(p => p.UserId).ToList();
        }

        public async Task<ApiResponseT<List<ChatMemberDto>>> GetTenantMembersAsync(CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;

            var users = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.IsApproved && u.Id != myId, ct);

            var dtos = users.Select(u => new ChatMemberDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Role = u.Role.ToString(),
            }).OrderBy(u => u.FullName).ToList();

            return ApiResponseT<List<ChatMemberDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponseT<ConversationDto>> GetOrCreateDMAsync(string otherUserId, CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;

            // Check if DM already exists between the two users
            var all = await _uow.Read<ChatConversation>().ListAsync(
                c => c.TenantId == tenantId && !c.IsGroup, ct);

            ChatConversation? existing = null;
            foreach (var conv in all)
            {
                var parts = await _uow.Read<ChatParticipant>()
                    .ListAsync(p => p.ConversationId == conv.Id, ct);
                var ids = parts.Select(p => p.UserId).ToHashSet();
                if (ids.Contains(myId) && ids.Contains(otherUserId) && ids.Count == 2)
                {
                    existing = conv;
                    break;
                }
            }

            if (existing != null)
                return ApiResponseT<ConversationDto>.SuccessResponse(await BuildConversationDtoAsync(existing, ct));

            var other = await _uow.Read<ApplicationUser>().GetSingleAsync(
                u => u.Id == otherUserId && u.TenantId == tenantId && u.IsApproved, ct);
            if (other == null)
                return ApiResponseT<ConversationDto>.FailureResponse("User not found", HttpStatusCode.NotFound);

            var conv2 = new ChatConversation(tenantId, false, myId);
            await _uow.Write<ChatConversation>().AddAsync(conv2, ct);

            await _uow.Write<ChatParticipant>().AddAsync(new ChatParticipant { ConversationId = conv2.Id, UserId = myId }, ct);
            await _uow.Write<ChatParticipant>().AddAsync(new ChatParticipant { ConversationId = conv2.Id, UserId = otherUserId }, ct);
            await _uow.SaveChangesAsync();

            return ApiResponseT<ConversationDto>.SuccessResponse(await BuildConversationDtoAsync(conv2, ct));
        }

        public async Task<ApiResponseT<ConversationDto>> CreateGroupAsync(CreateConversationDto dto, CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;

            var conv = new ChatConversation(tenantId, true, myId, dto.Name ?? "Group");
            await _uow.Write<ChatConversation>().AddAsync(conv, ct);

            var allIds = dto.ParticipantIds.Distinct().ToList();
            if (!allIds.Contains(myId)) allIds.Add(myId);

            foreach (var uid in allIds)
                await _uow.Write<ChatParticipant>().AddAsync(new ChatParticipant { ConversationId = conv.Id, UserId = uid }, ct);

            await _uow.SaveChangesAsync();
            return ApiResponseT<ConversationDto>.SuccessResponse(await BuildConversationDtoAsync(conv, ct));
        }

        public async Task<ApiResponseT<List<ConversationDto>>> GetMyConversationsAsync(CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;

            var myParticipations = await _uow.Read<ChatParticipant>()
                .ListAsync(p => p.UserId == myId, ct);
            var convIds = myParticipations.Select(p => p.ConversationId).ToList();

            var conversations = await _uow.Read<ChatConversation>()
                .ListAsync(c => convIds.Contains(c.Id), ct);

            var dtos = new List<ConversationDto>();
            foreach (var conv in conversations.OrderByDescending(c => c.UpdatedAt))
                dtos.Add(await BuildConversationDtoAsync(conv, ct));

            return ApiResponseT<List<ConversationDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponseT<List<MessageDto>>> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var participant = await _uow.Read<ChatParticipant>()
                .GetSingleAsync(p => p.ConversationId == conversationId && p.UserId == myId, ct);
            if (participant == null)
                return ApiResponseT<List<MessageDto>>.FailureResponse("Not a member", HttpStatusCode.Forbidden);

            var messages = await _uow.Read<ChatMessage>()
                .ListAsync(m => m.ConversationId == conversationId, ct);

            var ordered = messages.OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .OrderBy(m => m.CreatedAt).ToList();

            var senderIds = ordered.Select(m => m.SenderId).Distinct().ToList();
            var senders = await _uow.Read<ApplicationUser>().ListAsync(u => senderIds.Contains(u.Id), ct);
            var senderMap = senders.ToDictionary(u => u.Id);

            var dtos = ordered.Select(m =>
            {
                var sender = senderMap.GetValueOrDefault(m.SenderId);
                return new MessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderName = sender?.FullName ?? sender?.UserName ?? "",
                    SenderAvatar = null,
                    Type = m.Type,
                    TextContent = m.TextContent,
                    FileUrl = m.FileUrl,
                    FileName = m.FileName,
                    FileSizeBytes = m.FileSizeBytes,
                    SentAt = m.CreatedAt,
                    IsMine = m.SenderId == myId,
                };
            }).ToList();

            return ApiResponseT<List<MessageDto>>.SuccessResponse(dtos);
        }

        public Task<ApiResponseT<MessageDto>> SendMessageFromHubAsync(SendMessageDto dto, string senderId, Guid tenantId, CancellationToken ct = default)
            => SendMessageCoreAsync(dto, senderId, tenantId, ct);

        public Task<ApiResponseT<MessageDto>> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default)
            => SendMessageCoreAsync(dto, _currentUser.UserId, _currentUser.TenantId, ct);

        private async Task<ApiResponseT<MessageDto>> SendMessageCoreAsync(SendMessageDto dto, string myId, Guid tenantId, CancellationToken ct)
        {

            var participant = await _uow.Read<ChatParticipant>()
                .GetSingleAsync(p => p.ConversationId == dto.ConversationId && p.UserId == myId, ct);
            if (participant == null)
                return ApiResponseT<MessageDto>.FailureResponse("Not a member", HttpStatusCode.Forbidden);

            var msg = new ChatMessage(dto.ConversationId, tenantId, myId, dto.Type,
                dto.TextContent, dto.FileUrl, dto.FileName, dto.FileSizeBytes);

            await _uow.Write<ChatMessage>().AddAsync(msg, ct);

            // Update conversation UpdatedAt
            var conv = await _uow.Read<ChatConversation>().GetByIdAsync(dto.ConversationId, ct);
            conv?.MarkUpdated();
            if (conv != null) await _uow.Write<ChatConversation>().UpdateAsync(conv, ct);

            await _uow.SaveChangesAsync();

            var me = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == myId, ct);

            var msgDto = new MessageDto
            {
                Id = msg.Id,
                ConversationId = msg.ConversationId,
                SenderId = myId,
                SenderName = me?.FullName ?? me?.UserName ?? "",
                Type = msg.Type,
                TextContent = msg.TextContent,
                FileUrl = msg.FileUrl,
                FileName = msg.FileName,
                FileSizeBytes = msg.FileSizeBytes,
                SentAt = msg.CreatedAt,
                IsMine = true,
            };

            return ApiResponseT<MessageDto>.SuccessResponse(msgDto);
        }

        public async Task<ApiResponse> MarkReadAsync(Guid conversationId, CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var participant = await _uow.Read<ChatParticipant>()
                .GetSingleAsync(p => p.ConversationId == conversationId && p.UserId == myId, ct);
            if (participant == null) return ApiResponse.SuccessResponse();

            // ReadRepository is AsNoTracking — must go through WriteRepository to persist the change
            participant.LastReadAt = DateTime.UtcNow;
            await _uow.Write<ChatParticipant>().UpdateAsync(participant, ct);
            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse();
        }

        public async Task<ApiResponse> AddMembersAsync(Guid conversationId, AddGroupMembersDto dto, CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var conv = await _uow.Read<ChatConversation>().GetByIdAsync(conversationId, ct);
            if (conv == null || !conv.IsGroup)
                return ApiResponse.FailureResponse("Group not found", HttpStatusCode.NotFound);

            var existing = await _uow.Read<ChatParticipant>()
                .ListAsync(p => p.ConversationId == conversationId, ct);
            var existingIds = existing.Select(p => p.UserId).ToHashSet();

            foreach (var uid in dto.UserIds.Where(id => !existingIds.Contains(id)))
                await _uow.Write<ChatParticipant>().AddAsync(new ChatParticipant { ConversationId = conversationId, UserId = uid }, ct);

            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse();
        }

        public async Task<ApiResponse> LeaveGroupAsync(Guid conversationId, CancellationToken ct = default)
        {
            var myId = _currentUser.UserId;
            var participant = await _uow.Read<ChatParticipant>()
                .GetSingleAsync(p => p.ConversationId == conversationId && p.UserId == myId, ct);
            if (participant == null) return ApiResponse.SuccessResponse();

            await _uow.Write<ChatParticipant>().DeleteAsync(participant, ct);
            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse();
        }

        private async Task<ConversationDto> BuildConversationDtoAsync(ChatConversation conv, CancellationToken ct)
        {
            var myId = _currentUser.UserId;
            var participants = await _uow.Read<ChatParticipant>()
                .ListAsync(p => p.ConversationId == conv.Id, ct);
            var userIds = participants.Select(p => p.UserId).ToList();
            var users = await _uow.Read<ApplicationUser>().ListAsync(u => userIds.Contains(u.Id), ct);
            var userMap = users.ToDictionary(u => u.Id);

            var participantDtos = participants.Select(p =>
            {
                var u = userMap.GetValueOrDefault(p.UserId);
                return new ParticipantDto
                {
                    UserId = p.UserId,
                    FullName = u?.FullName ?? u?.UserName ?? "",
                    AvatarUrl = u?.ProfileImageUrl,
                    Role = u?.Role.ToString() ?? "",
                };
            }).ToList();

            var messages = await _uow.Read<ChatMessage>()
                .ListAsync(m => m.ConversationId == conv.Id, ct);

            var lastMsg = messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var myParticipant = participants.FirstOrDefault(p => p.UserId == myId);
            var unread = myParticipant?.LastReadAt == null
                ? messages.Count(m => m.SenderId != myId)
                : messages.Count(m => m.SenderId != myId && m.CreatedAt > myParticipant.LastReadAt);

            MessageDto? lastMsgDto = null;
            if (lastMsg != null)
            {
                var sender = userMap.GetValueOrDefault(lastMsg.SenderId);
                lastMsgDto = new MessageDto
                {
                    Id = lastMsg.Id,
                    ConversationId = lastMsg.ConversationId,
                    SenderId = lastMsg.SenderId,
                    SenderName = sender?.FullName ?? "",
                    Type = lastMsg.Type,
                    TextContent = lastMsg.TextContent,
                    FileUrl = lastMsg.FileUrl,
                    FileName = lastMsg.FileName,
                    SentAt = lastMsg.CreatedAt,
                    IsMine = lastMsg.SenderId == myId,
                };
            }

            // For DM, derive name from the other participant
            string? name = conv.Name;
            if (!conv.IsGroup)
            {
                var other = participantDtos.FirstOrDefault(p => p.UserId != myId);
                name = other?.FullName ?? name;
            }

            return new ConversationDto
            {
                Id = conv.Id,
                IsGroup = conv.IsGroup,
                Name = name,
                AvatarUrl = conv.AvatarUrl,
                Participants = participantDtos,
                LastMessage = lastMsgDto,
                UnreadCount = unread,
                UpdatedAt = conv.UpdatedAt,
            };
        }
    }
}
