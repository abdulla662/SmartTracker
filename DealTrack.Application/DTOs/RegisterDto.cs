using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs
{
    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public SubscriptionPlan SubscriptionPlan { get; set; }
        /// <summary>
        /// Only used when registering under an existing company (join request).
        /// Defaults to Sales if not provided.
        /// </summary>
        public UserRole? RequestedRole { get; set; }
    }
}
