using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public string UserId { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }

        private RefreshToken() { UserId = string.Empty; Token = string.Empty; }

        public RefreshToken(Guid tenantId, string userId, string token, DateTime expiresAt)
        {
            TenantId = tenantId;
            UserId = userId;
            Token = token;
            ExpiresAt = expiresAt;
            IsRevoked = false;
        }

        public void Revoke() { IsRevoked = true; MarkUpdated(); }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
