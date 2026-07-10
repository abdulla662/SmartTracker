using System.ComponentModel.DataAnnotations;

namespace DealTrack.Application.DTOs.Payments
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid ClientId { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public string? Notes { get; set; }

        // Optional: set or update the total deal amount for this client
        public decimal? TotalDealAmount { get; set; }
    }
}
