namespace DealTrack.Application.DTOs.Ocr
{
    public class OcrClientDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class CheckPhonesRequest
    {
        public List<string> Phones { get; set; } = new();
    }
}
