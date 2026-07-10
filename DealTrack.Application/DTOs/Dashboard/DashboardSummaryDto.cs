namespace DealTrack.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        // ── Scalar KPIs ────────────────────────────────────────────────────
        public int     TotalClientsCount     { get; set; }
        public int     TodayFollowUpsCount   { get; set; }
        public int     OverdueFollowUpsCount { get; set; }
        public int     PendingFollowUpsCount { get; set; }
        public int     CompletedTodayCount   { get; set; }
        public decimal TotalPaidAmount       { get; set; }
        public int     TotalPaymentsCount    { get; set; }

        // ── Status breakdown (for pie / donut chart) ───────────────────────
        public int FollowUpsDone   { get; set; }
        public int FollowUpsMissed { get; set; }

        // ── Chart series ───────────────────────────────────────────────────
        public List<MonthlyRevenueDto>  MonthlyRevenue   { get; set; } = new();
        public List<TeamMemberStatDto>  TeamMemberStats  { get; set; } = new();
    }

    public class MonthlyRevenueDto
    {
        public int     Year      { get; set; }
        public int     Month     { get; set; }
        public string  MonthName { get; set; } = string.Empty;
        public decimal Amount    { get; set; }
        public int     Count     { get; set; }
    }

    public class TeamMemberStatDto
    {
        public string  MemberName      { get; set; } = string.Empty;
        public int     RoleId          { get; set; }
        public int     ClientsCount    { get; set; }
        public decimal Revenue         { get; set; }
        public int     FollowUpsDone   { get; set; }
        public int     FollowUpsPending { get; set; }
        public int     FollowUpsOverdue { get; set; }
    }
}
