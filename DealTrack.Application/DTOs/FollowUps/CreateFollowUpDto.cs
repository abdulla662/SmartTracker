using System.ComponentModel.DataAnnotations;

namespace DealTrack.Application.DTOs.FollowUps
{
    public class CreateFollowUpDto
    {
        [Required]
        public Guid ClientId { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        public string? Notes { get; set; }
    }
}
