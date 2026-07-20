using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.Targets
{
    public class TargetResponseDto
    {
        public Guid Id { get; set; }
        public string AssignedToUserId { get; set; } = "";
        public string AssignedToUserName { get; set; } = "";
        public string AssignedToUserRole { get; set; } = "";
        public string CreatedByUserId { get; set; } = "";
        public string CreatedByUserName { get; set; } = "";
        public TargetType TargetType { get; set; }
        public string? CustomTypeName { get; set; }
        public string? CustomTypeUnit { get; set; }
        public decimal? Value { get; set; }
        public decimal CurrentProgress { get; set; }
        public decimal ProgressPercentage { get; set; }
        public bool IsAchieved { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
