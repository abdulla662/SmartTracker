using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Chat;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IWebHostEnvironment _env;

        public ChatController(IChatService chatService, IWebHostEnvironment env)
        {
            _chatService = chatService;
            _env = env;
        }

        [HttpGet("members")]
        public async Task<ApiResponseT<List<ChatMemberDto>>> GetMembers(CancellationToken ct)
            => await _chatService.GetTenantMembersAsync(ct);

        [HttpGet("conversations")]
        public async Task<ApiResponseT<List<ConversationDto>>> GetConversations(CancellationToken ct)
            => await _chatService.GetMyConversationsAsync(ct);

        [HttpPost("dm/{otherUserId}")]
        public async Task<ApiResponseT<ConversationDto>> GetOrCreateDM(string otherUserId, CancellationToken ct)
            => await _chatService.GetOrCreateDMAsync(otherUserId, ct);

        [HttpPost("group")]
        public async Task<ApiResponseT<ConversationDto>> CreateGroup([FromBody] CreateConversationDto dto, CancellationToken ct)
            => await _chatService.CreateGroupAsync(dto, ct);

        [HttpGet("{conversationId}/messages")]
        public async Task<ApiResponseT<List<MessageDto>>> GetMessages(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
            => await _chatService.GetMessagesAsync(conversationId, page, pageSize, ct);

        [HttpPost("{conversationId}/messages")]
        public async Task<ApiResponseT<MessageDto>> SendMessage(Guid conversationId, [FromBody] SendMessageDto dto, CancellationToken ct)
        {
            dto.ConversationId = conversationId;
            return await _chatService.SendMessageAsync(dto, ct);
        }

        [HttpPost("{conversationId}/read")]
        public async Task<ApiResponse> MarkRead(Guid conversationId, CancellationToken ct)
            => await _chatService.MarkReadAsync(conversationId, ct);

        [HttpPost("{conversationId}/members")]
        public async Task<ApiResponse> AddMembers(Guid conversationId, [FromBody] AddGroupMembersDto dto, CancellationToken ct)
            => await _chatService.AddMembersAsync(conversationId, dto, ct);

        [HttpPost("{conversationId}/leave")]
        public async Task<ApiResponse> LeaveGroup(Guid conversationId, CancellationToken ct)
            => await _chatService.LeaveGroupAsync(conversationId, ct);

        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp",
            ".mp4", ".webm", ".mov",
            ".mp3", ".ogg", ".m4a", ".wav",
            ".pdf", ".docx", ".xlsx", ".pptx", ".txt"
        };

        // Magic-byte signatures for allowed types — prevents renaming a .html to .jpg
        private static readonly Dictionary<string, byte[]> _magicBytes = new()
        {
            { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            { ".gif",  new byte[] { 0x47, 0x49, 0x46 } },
            { ".webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } },
            { ".pdf",  new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            { ".mp4",  new byte[] { 0x00, 0x00, 0x00 } }, // partial — containers vary
        };

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            const long maxSize = 50 * 1024 * 1024;
            if (file.Length > maxSize)
                return BadRequest(new { message = "File too large (max 50 MB)" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                return BadRequest(new { message = "File type not allowed." });

            // Validate magic bytes for image/pdf types
            if (_magicBytes.TryGetValue(ext, out var magic))
            {
                var header = new byte[magic.Length];
                using var peek = file.OpenReadStream();
                await peek.ReadAsync(header, 0, header.Length, ct);
                if (!header.Take(magic.Length).SequenceEqual(magic))
                    return BadRequest(new { message = "File content does not match its extension." });
            }

            var uploadsPath = Path.Combine(_env.WebRootPath ?? "/app/wwwroot", "chat-uploads");
            Directory.CreateDirectory(uploadsPath);

            // Guid name — no user-controlled characters in the stored filename
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream, ct);

            var url = $"/chat-uploads/{fileName}";
            return Ok(new { url, fileName = file.FileName, size = file.Length });
        }
    }
}
