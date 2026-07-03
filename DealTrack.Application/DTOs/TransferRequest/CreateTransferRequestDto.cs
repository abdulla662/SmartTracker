namespace DealTrack.Application.DTOs.TransferRequest
{
    public class CreateTransferRequestDto
    {
        public string SalesUserId { get; set; } = string.Empty;
        public string ToTeamLeadId { get; set; } = string.Empty;
    }
}