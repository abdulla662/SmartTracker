namespace DealTrack.Application.DTOs.Clients
{
    public class ImportResultDto
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
    }

    public class ImportRowError
    {
        public int Row { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
