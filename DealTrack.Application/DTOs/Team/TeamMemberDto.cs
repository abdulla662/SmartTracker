namespace DealTrack.Application.DTOs.Team
{
    public class TeamMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
    }
}
