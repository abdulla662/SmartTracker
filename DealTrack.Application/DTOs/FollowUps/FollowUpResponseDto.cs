using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.FollowUps
{
    public class FollowUpResponseDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }
        public FollowUpStatus Status { get; set; }
        public string? Notes { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
