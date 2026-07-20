using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class SalaryAdjustment : BaseEntity
    {
        public string UserId { get; private set; }
        public Guid TenantId { get; private set; }
        public int Month { get; private set; }
        public int Year { get; private set; }
        public decimal Amount { get; private set; }
        public AdjustmentType Type { get; private set; }
        public string Description { get; private set; }
        public string CreatedByUserId { get; private set; }

        private SalaryAdjustment() { UserId = string.Empty; Description = string.Empty; CreatedByUserId = string.Empty; }

        public SalaryAdjustment(string userId, Guid tenantId, int month, int year, decimal amount, AdjustmentType type, string description, string createdByUserId)
        {
            UserId = userId;
            TenantId = tenantId;
            Month = month;
            Year = year;
            Amount = amount;
            Type = type;
            Description = description;
            CreatedByUserId = createdByUserId;
        }
    }
}
