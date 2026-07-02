namespace DealTrack.Application.DTOs.Team
{
    public class TeamLeadWithMembersDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<TeamMemberDto> SalesMembers { get; set; } = new();
    }
}
