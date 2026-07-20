using DealTrack.Application.DTOs.Chat;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DealTrack.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        }

        public async Task SendMessage(SendMessageDto dto)
        {
            // IHttpContextAccessor.HttpContext is null in SignalR hub context,
            // so we extract identity directly from the hub's Context.User
            var userId   = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var tenantStr = Context.User?.FindFirst("TenantId")?.Value ?? "";
            if (!Guid.TryParse(tenantStr, out var tenantId) || string.IsNullOrEmpty(userId))
                return;

            var result = await _chatService.SendMessageFromHubAsync(dto, userId, tenantId);
            if (result.Success && result.Data != null)
            {
                // Broadcast to everyone currently viewing this conversation
                await Clients.Group($"conv_{dto.ConversationId}")
                    .SendAsync("ReceiveMessage", result.Data);

                // Push to each OTHER participant's personal group for sidebar badge
                // (sender already got ReceiveMessage via conv_ group — skip them)
                var participantIds = await _chatService.GetParticipantIdsAsync(dto.ConversationId);
                foreach (var uid in participantIds.Where(id => id != userId))
                    await Clients.Group($"user_{uid}")
                        .SendAsync("NewChatMessage", result.Data);
            }
        }

        public async Task MarkRead(string conversationId)
        {
            if (Guid.TryParse(conversationId, out var id))
                await _chatService.MarkReadAsync(id); // REST fallback handles hub context via REST API
        }

        public async Task Typing(string conversationId, bool isTyping)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await Clients.OthersInGroup($"conv_{conversationId}")
                .SendAsync("UserTyping", new { userId, conversationId, isTyping });
        }
    }
}
