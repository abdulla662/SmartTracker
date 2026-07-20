using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.HR
{
    public class CreateHRActionDto
    {
        public string TargetUserId { get; set; } = string.Empty;
        public HRActionType ActionType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? TargetTeamLeadId { get; set; }
        public decimal? Amount { get; set; }
        public Guid? JoinRequestId { get; set; }
    }
}
