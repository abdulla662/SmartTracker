using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.HR
{
    public class HRActionResponseDto
    {
        public Guid Id { get; set; }
        public string RequestedByUserId { get; set; } = string.Empty;
        public string RequestedByUserName { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public string TargetUserName { get; set; } = string.Empty;
        public string? TargetTeamLeadId { get; set; }
        public string? TargetTeamLeadName { get; set; }
        public HRActionType ActionType { get; set; }
        public string Description { get; set; } = string.Empty;
        public HRActionStatus Status { get; set; }
        public string? ReviewedByAdminName { get; set; }
        public string? AdminNote { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal? Amount { get; set; }
        public Guid? JoinRequestId { get; set; }
    }
}
