namespace DealTrack.Application.DTOs.TenantInvite
{
    public class InviteInfoDto
    {
        public string Email { get; set; } = string.Empty;
        public string InviterName { get; set; } = string.Empty;
        public string InvitedRole { get; set; } = string.Empty;
    }
}
