using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.Salary
{
    public class SalaryAdjustmentDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public AdjustmentType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
