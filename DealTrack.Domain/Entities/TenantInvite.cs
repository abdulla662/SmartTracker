using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class TenantInvite : BaseEntity
    {
        public string InviteCode { get; private set; }
        public Guid TenantId { get; private set; }
        public Tenant Tenant { get; private set; } = null!;
        public string Email { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }
        public Guid? TeamLeadId { get; private set; }
        public UserRole InvitedRole { get; private set; }
        public string? InvitedByUserId { get; private set; }

        private TenantInvite() { InviteCode = string.Empty; Email = string.Empty; }

        public TenantInvite(Guid tenantId, string email, Guid? teamLeadId = null, UserRole invitedRole = UserRole.Sales, string? invitedByUserId = null)
        {
            TenantId = tenantId;
            Email = email;
            TeamLeadId = teamLeadId;
            InvitedRole = invitedRole;
            InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
            ExpiresAt = DateTime.UtcNow.AddDays(7);
            IsUsed = false;
            InvitedByUserId = invitedByUserId;
        }

        public void MarkUsed()
        {
            IsUsed = true;
            MarkUpdated();
        }
    }
}