using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.Feedback
{
    public class FeedbackTargetDto
    {
        public Guid Id { get; set; }
        public TargetType TargetType { get; set; }
        public string? CustomTypeName { get; set; }
        public string? CustomTypeUnit { get; set; }
        public decimal? GoalValue { get; set; }
        public decimal CurrentProgress { get; set; }
        public decimal ProgressPercentage { get; set; }
        public bool IsAchieved { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
