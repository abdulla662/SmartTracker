using DealTrack.Application.Common;
using DealTrack.Domain.Entities;
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

        public void ApplyFilter(RequestFilterContext ctx) => _filterContext = ctx;

        public bool ApplySoftDeleteFilter => _filterContext?.ApplySoftDeleteFilter ?? false;

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

            base.OnModelCreating(modelBuilder);
        }
    }
}
