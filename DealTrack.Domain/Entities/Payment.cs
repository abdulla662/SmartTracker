using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public Guid ClientId { get; private set; }
        public Client Client { get; private set; }

        public decimal Amount { get; private set; }
        public DateTime PaymentDate { get; private set; }

        private Payment() { }

        public Payment(Guid tenantId, Guid clientId, decimal amount)
        {
            TenantId = tenantId;
            ClientId = clientId;
            Amount = amount;
            PaymentDate = DateTime.UtcNow;
        }
    }
}
