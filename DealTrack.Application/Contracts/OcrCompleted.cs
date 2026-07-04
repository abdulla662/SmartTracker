namespace DealTrack.Application.Contracts
{
    public class OcrCompleted
    {
        public string ExtractedText { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
