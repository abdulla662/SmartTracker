namespace DealTrack.Application.DTOs.Profile
{
    public class GetProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
    }
}
