namespace DealTrack.Application.DTOs.Team
{
    public class TeamsResponseDto
    {
        public List<TeamLeadWithMembersDto> TeamLeads { get; set; } = new();
        public List<TeamMemberDto> IndividualSales { get; set; } = new();
        public List<TeamMemberDto> HrMembers { get; set; } = new();
        public List<TeamMemberDto> Accountants { get; set; } = new();
        public List<TeamMemberDto> Admins { get; set; } = new();
    }
}
