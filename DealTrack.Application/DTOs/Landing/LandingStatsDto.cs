namespace DealTrack.Application.DTOs.Landing
{
    public class LandingStatsDto
    {
        public int    ActiveClients      { get; set; }
        public int    TodaysTasks        { get; set; }
        public decimal CollectedThisMonth { get; set; }
        public double CollectionRate     { get; set; }
    }
}
