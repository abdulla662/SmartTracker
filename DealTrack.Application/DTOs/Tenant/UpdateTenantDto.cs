namespace DealTrack.Application.DTOs.Tenant
{
    public class UpdateTenantDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Industry { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string? Currency { get; set; }
        public string? Timezone { get; set; }
    }
}
