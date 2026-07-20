using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities
{
    public class MonthlySalary : BaseEntity
    {
        public string UserId { get; private set; }
        public Guid TenantId { get; private set; }
        public decimal BaseSalary { get; private set; }
        public int Month { get; private set; }
        public int Year { get; private set; }
        public string SetByUserId { get; private set; }

        private MonthlySalary() { UserId = string.Empty; SetByUserId = string.Empty; }

        public MonthlySalary(string userId, Guid tenantId, decimal baseSalary, int month, int year, string setByUserId)
        {
            UserId = userId;
            TenantId = tenantId;
            BaseSalary = baseSalary;
            Month = month;
            Year = year;
            SetByUserId = setByUserId;
        }

        public void Update(decimal baseSalary, string setByUserId)
        {
            BaseSalary = baseSalary;
            SetByUserId = setByUserId;
            MarkUpdated();
        }
    }
}
