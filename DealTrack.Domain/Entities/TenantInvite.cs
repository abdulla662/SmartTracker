using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities  
{
    public class TenantInvite : BaseEntity
    {
        public string InviteCode { get; private set; }
        public Guid TenantId { get; private set; }
        public Tenant Tenant { get; private set; } = null!;  
        public string Email { get;  set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }
        


        private TenantInvite() { InviteCode = string.Empty; Email = string.Empty; }

        public TenantInvite(Guid tenantId, string email)
        {
            TenantId = tenantId;
            Email = email;
            InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
            ExpiresAt = DateTime.UtcNow.AddDays(7);
            IsUsed = false;
        }

        public void MarkUsed()
        {
            IsUsed = true;
            MarkUpdated();
        }
    }
}