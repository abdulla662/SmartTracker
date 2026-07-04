namespace DealTrack.Application.Contracts
{
    public class OcrRequested
    {
        public string ImageBase64 { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
