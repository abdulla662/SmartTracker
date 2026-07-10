using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.FollowUps
{
    public class UpdateFollowUpDto
    {
        public DateTime FollowUpDate { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public FollowUpStatus? Status { get; set; }
    }
}
