namespace DealTrack.Application.DTOs.Tenant
{
    public class TenantInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Industry { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string Currency { get; set; } = "EGP";
        public string Timezone { get; set; } = "Africa/Cairo";
        public bool IsPersonal { get; set; }
        public string Plan { get; set; } = string.Empty;
    }
}
