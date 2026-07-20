namespace DealTrack.Application.DTOs.Auth
{
    public class CancelPendingRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
