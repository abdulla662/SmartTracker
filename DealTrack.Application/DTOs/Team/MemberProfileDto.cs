namespace DealTrack.Application.DTOs.Team
{
    public class MemberProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalClients { get; set; }
        public int PendingFollowUps { get; set; }
        public int DoneFollowUps { get; set; }
        public int MissedFollowUps { get; set; }
        public decimal TotalPayments { get; set; }
        public int PaymentCount { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }
}
