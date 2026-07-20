namespace DealTrack.Application.DTOs.Profile
{
    public class GetProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public bool IsIndividual { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }
}
