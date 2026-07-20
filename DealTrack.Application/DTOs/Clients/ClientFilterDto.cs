namespace DealTrack.Application.DTOs.Clients
{
    public class ClientFilterDto
    {
        public string? Search { get; set; }
        public string? AssignedToUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
