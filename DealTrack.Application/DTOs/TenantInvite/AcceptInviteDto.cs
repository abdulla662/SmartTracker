namespace DealTrack.Application.DTOs.TenantInvite
{
    public class AcceptInviteDto
    {
        public string InviteCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
