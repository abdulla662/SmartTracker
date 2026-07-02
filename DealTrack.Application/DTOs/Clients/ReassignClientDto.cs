namespace DealTrack.Application.DTOs.Clients
{
    public class ReassignClientDto
    {
        public string NewSalesUserId { get; set; } = string.Empty;
        public Guid? NewTenantId { get; set; }
    }
}
