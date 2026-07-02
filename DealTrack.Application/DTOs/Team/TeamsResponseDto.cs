namespace DealTrack.Application.DTOs.Team
{
    public class TeamsResponseDto
    {
        public List<TeamLeadWithMembersDto> TeamLeads { get; set; } = new();
        public List<TeamMemberDto> IndividualSales { get; set; } = new();
    }
}
