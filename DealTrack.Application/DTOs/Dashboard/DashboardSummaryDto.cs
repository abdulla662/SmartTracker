using DealTrack.Application.DTOs.FollowUps;

namespace DealTrack.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int TodayFollowUpsCount { get; set; }
        public int OverdueFollowUpsCount { get; set; }
        public int PendingFollowUpsCount { get; set; }
        public int CompletedTodayCount { get; set; }
        public int TotalClientsCount { get; set; }

        public decimal TotalPaidAmount { get; set; }
        public int TotalPaymentsCount { get; set; }

        public List<FollowUpResponseDto> TodayFollowUps { get; set; } = new();
        public List<FollowUpResponseDto> OverdueFollowUps { get; set; } = new();
    }
}
