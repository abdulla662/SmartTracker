namespace DealTrack.Application.DTOs.TenantInvite
{
    public class GetInviteDto 
    {
        public string Email { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}