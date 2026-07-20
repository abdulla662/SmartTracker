using DealTrack.Application.Common;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DealTrack.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private RequestFilterContext? _filterContext;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<FollowUp> FollowUps => Set<FollowUp>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
        public DbSet<ClientFinancialSummary> ClientFinancialSummaries => Set<ClientFinancialSummary>();
        public DbSet<TenantInvite> TenantInvites => Set<TenantInvite>();
        public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<Target> Targets => Set<Target>();
        public DbSet<MonthlySalary> MonthlySalaries => Set<MonthlySalary>();
        public DbSet<SalaryAdjustment> SalaryAdjustments => Set<SalaryAdjustment>();
        public DbSet<HRActionRequest> HRActionRequests => Set<HRActionRequest>();
        public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
        public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        public void ApplyFilter(RequestFilterContext ctx) => _filterContext = ctx;

        public bool ApplySoftDeleteFilter => _filterContext?.ApplySoftDeleteFilter ?? true;

        private Guid? CurrentTenantId
        {
            get
            {
                var claim = _httpContextAccessor?.HttpContext?.User
                    ?.FindFirst("TenantId");
                return Guid.TryParse(claim?.Value, out var id) ? id : null;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            modelBuilder.Entity<Client>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<FollowUp>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<Payment>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<ActivityLog>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<Notification>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<ClientFinancialSummary>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<TransferRequest>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<Target>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<MonthlySalary>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<SalaryAdjustment>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<HRActionRequest>().HasQueryFilter(x =>
                (!ApplySoftDeleteFilter || !x.IsDeleted) &&
                (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            // Chat
            modelBuilder.Entity<ChatConversation>().HasQueryFilter(x =>
                !x.IsDeleted && (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<ChatMessage>().HasQueryFilter(x =>
                !x.IsDeleted && (CurrentTenantId == null || x.TenantId == CurrentTenantId));

            modelBuilder.Entity<ChatParticipant>()
                .HasKey(p => new { p.ConversationId, p.UserId });

            modelBuilder.Entity<ChatParticipant>()
                .HasOne(p => p.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(p => p.ConversationId);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
