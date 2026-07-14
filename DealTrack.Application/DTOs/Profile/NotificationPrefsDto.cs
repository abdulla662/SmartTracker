namespace DealTrack.Application.DTOs.Profile
{
    public class NotificationPrefsDto
    {
        public bool EmailFollowUps  { get; set; }
        public bool EmailPayments   { get; set; }
        public bool EmailSystem     { get; set; }
        public bool PushFollowUps   { get; set; }
        public bool PushPayments    { get; set; }
        public bool PushOverdue     { get; set; }
        public bool DailyDigest     { get; set; }
        public bool WeeklyReport    { get; set; }
    }
}
