namespace DealTrack.Application.DTOs.Feedback
{
    public class FeedbackMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public List<FeedbackTargetDto> Targets { get; set; } = new();
    }
}
