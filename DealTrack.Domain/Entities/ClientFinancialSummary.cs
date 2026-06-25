using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class ClientFinancialSummary : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public Guid ClientId { get; private set; }

        public decimal TotalAmount { get; private set; }
        public decimal PaidAmount { get; private set; }
        public decimal Remaining => TotalAmount - PaidAmount;
        public PaymentStatus Status { get; private set; }

        private ClientFinancialSummary() { }

        public ClientFinancialSummary(Guid tenantId, Guid clientId, decimal totalAmount)
        {
            TenantId = tenantId;
            ClientId = clientId;
            TotalAmount = totalAmount;
            PaidAmount = 0;
            Status = PaymentStatus.Pending;
        }

        public void AddPayment(decimal amount)
        {
            PaidAmount += amount;
            Status = PaidAmount >= TotalAmount ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
            MarkUpdated();
        }

        public void SetTotalAmount(decimal total)
        {
            TotalAmount = total;
            Status = PaidAmount >= TotalAmount ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
            MarkUpdated();
        }
    }
}
