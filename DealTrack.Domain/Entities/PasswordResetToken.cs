using DealTrack.Domain.Common;

namespace DealTrack.Domain.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public string UserId { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }

        private PasswordResetToken() { UserId = string.Empty; Token = string.Empty; }

        public PasswordResetToken(string userId, string token, DateTime expiresAt)
        {
            UserId = userId;
            Token = token;
            ExpiresAt = expiresAt;
            IsUsed = false;
        }

        public void MarkUsed() { IsUsed = true; MarkUpdated(); }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsValid => !IsUsed && !IsExpired;
    }
}