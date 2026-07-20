using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.Salary
{
    public class AddAdjustmentDto
    {
        public string UserId { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Amount { get; set; }
        public AdjustmentType Type { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
