namespace DealTrack.Application.DTOs.TenantInvite
{
    public class CreateInviteDto
    {
        public string Email { get; set; } = string.Empty;
        public Guid? TeamLeadId { get; set; }
    }
}
