using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.TransferRequest
{
    public class TransferRequestResponseDto
    {
        public Guid Id { get; set; }
        public string SalesUserId { get; set; } = string.Empty;
        public string SalesFullName { get; set; } = string.Empty;
        public string FromTeamLeadId { get; set; } = string.Empty;
        public string FromTeamLeadName { get; set; } = string.Empty;
        public string ToTeamLeadId { get; set; } = string.Empty;
        public string ToTeamLeadName { get; set; } = string.Empty;
        public TransferRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}